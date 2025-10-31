namespace Yarn.Unity.Addons.DialogueWheel
{
    using System;
    using System.Linq;
    using System.Threading;
    using UnityEngine;
    using UnityEngine.EventSystems;
    using Yarn.Unity;

#nullable enable

    /// <summary>
    /// A <see cref="DialogueViewBase"/> subclass for handling options around a wheel.
    /// </summary>
    /// <remarks>
    /// The job of arranging and positioning options is within the various <see cref="DialogueWheelLayout"/> subclasses.
    /// </remarks>
    public class WheelDialogueView : DialoguePresenterBase
    {
        /// <summary>
        /// Whether this view is currently showing options.
        /// </summary>
        public bool IsShowingOptions { get; private set; }

        // the region index of the last selected option
        private int oldAngleIndex = -1;

        // the layout subclass for the wheel.
        // Does most of the heavy lifting
        [SerializeField] private DialogueWheelLayout? layout;

        [SerializeField] private PresentationController? presentationController;

        // the current regions, used to select/deselect options
        private OptionRegion[] regions = { };

        // Because its very likely you won't want to define an entire circles worth of regions
        // this handles the situation where you still want an option to show up as selected until
        // you unambigously move into the region of another option.
        // Set to true by default as I think this is what most people want and is what most games do
        [SerializeField] private bool RetainPreviousSelectedOptionInRegionVoids = true;

        // Are options that fail their conditional to be shown?
        [SerializeField] private bool ShowUnavailableOptions = false;

        // are options that fail their conditional allowed to be selected?
        // this is quite uncommon outside of testing
        [SerializeField] private bool AllowSelectingUnavailableOptions = false;

        // The amount of time after showing the wheel before allowing a
        // selection (in order to prevent inputs being counted twice on the same
        // frame, and to prevent button-mashing)
        [Min(0)]
        [SerializeField] private float DelayBeforeAllowingSelection = 0.1f;

        /// <summary>
        /// True if we're waiting for a timeout to elapse before accepting a
        /// signal to select an option.
        /// </summary>
        /// <seealso cref="DelayBeforeAllowingSelection"/>
        private bool waitingForSelectionDelay = false;

        /// <summary>
        /// The current angle the player has rotated for the selection.
        /// Public because we don't want the input directly tied to the dialogue view.
        /// Other things should be setting this value.
        /// </summary>
        public float inputAngle = 0;

        private YarnTaskCompletionSource<int>? optionSelectedCompletionSource;

        public override YarnTask OnDialogueStartedAsync()
        {
            return YarnTask.CompletedTask;
        }

        public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
        {
            return YarnTask.CompletedTask;
        }

        /// <inheritdoc />
        public override async YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, CancellationToken cancellationToken)
        {
            if (presentationController == null)
            {
                Debug.LogWarning($"unable to run options, {nameof(presentationController)} is null");
                return null;
            }

            if (layout == null)
            {
                Debug.LogWarning($"unable to run options, {nameof(layout)} is null");
                return null;
            }

            IsShowingOptions = true;

            // working out what options we need to send to the layout
            DialogueOption[] validOptions;

            if (ShowUnavailableOptions)
            {
                validOptions = dialogueOptions;
            }
            else
            {
                // need to remove all the dialogue options that have failed their conditional
                validOptions = dialogueOptions.AsEnumerable().Where(o => o.IsAvailable).ToArray();
            }

            regions = layout.CreateDialogueRegions(validOptions);

            presentationController.PresentOptionViews(layout.GetAllOptionViews());

            optionSelectedCompletionSource = new YarnTaskCompletionSource<int>();
            cancellationToken.Register(() =>
            {
                optionSelectedCompletionSource?.TrySetCanceled();
            });

            try
            {
                waitingForSelectionDelay = true;

                // Wait at least one frame to ensure that any input from the
                // last action (i.e. advancing to the next line) doesn't also
                // trigger a selection this frame
                await YarnTask.Yield();

                if (DelayBeforeAllowingSelection > 0)
                {
                    // Wait for the timeout
                    await YarnTask.Delay(TimeSpan.FromSeconds(DelayBeforeAllowingSelection), cancellationToken);
                }

                waitingForSelectionDelay = false;

                // wait until the user selects on the options
                int index = await optionSelectedCompletionSource.Task;
                return dialogueOptions[index];
            }
            catch (OperationCanceledException)
            {
                // if we were cancelled we don't need to log that, we just return null and clean up
                return null;
            }
            catch (Exception e)
            {
                // if the options were cancelled then return null
                Debug.LogException(e);
                return null;
            }
            finally
            {
                // in all cases we null out the completion source and clean up
                waitingForSelectionDelay = false;
                optionSelectedCompletionSource = null;
                IsShowingOptions = false;
            }
        }

        /// <summary>
        /// invokes the selected option action that was based over by the dialogue runner.
        /// </summary>
        /// <param name="optionIndex">The id of the option to select</param>
        private void SelectOption(int optionIndex)
        {
            if (waitingForSelectionDelay)
            {
                // We're waiting for the selection delay to time out. Ignore
                // this selection.
                return;
            }

            if (layout == null)
            {
                Debug.LogWarning($"unable to select an option, {nameof(layout)} is null");
                return;
            }

            if (optionSelectedCompletionSource == null)
            {
                Debug.LogError($"asked to select option {optionIndex} but have no completion source");
                return;
            }

            if (presentationController == null)
            {
                return;
            }

            presentationController.Dismiss();
            layout.DestroyDialogueOptionViews();

            oldAngleIndex = -1;
            regions = Array.Empty<OptionRegion>();

            optionSelectedCompletionSource.TrySetResult(optionIndex);
        }

        void Update()
        {
            var angle = inputAngle;

            if (layout == null)
            {
                Debug.LogWarning($"unable to update angle, {nameof(layout)} is null");
                return;
            }
            if (presentationController == null)
            {
                Debug.LogWarning($"unable to update angle, {nameof(presentationController)} is null");
                return;
            }

            presentationController.SetCurrentAngle(angle);

            // results can be negative angles, which are just annoying
            if (angle < 0)
            {
                angle += 360;
            }

            if (regions == null)
            {
                return;
            }
            if (regions.Length == 0)
            {
                return;
            }

            // working out the index of the region the cursor is in
            int angleIndex = CalculateButtonIndex(angle, regions);

            // we didn't move into a region
            if (angleIndex == -1)
            {
                if (RetainPreviousSelectedOptionInRegionVoids)
                {
                    // we allow for this though
                    // we just keep the currently selected element as is
                    return;
                }
                else
                {
                    // otherwise we deselect everything
                    oldAngleIndex = -1;
                    presentationController.SetHighlightedOptionView(null);
                    return;
                }
            }

            // if we haven't changed what is selected no need to do anything else
            if (oldAngleIndex == angleIndex)
            {
                return;
            }

            presentationController.SetHighlightedOptionView(layout.Option(angleIndex));

            oldAngleIndex = angleIndex;
        }

        /// <summary>
        /// Selects the currently highlighted option and tells the dialogue runner about this.
        /// </summary>
        /// <remarks>
        /// If no option is highlighted, or an invalid option is selected nothing will happen.
        /// </remarks>
        public void SelectCurrentlyHighlightedOption()
        {
            // if we have an invalid option we can't select it
            if (oldAngleIndex == -1)
            {
                return;
            }

            if (layout == null)
            {
                Debug.LogWarning($"unable to highlt option, {nameof(layout)} is null");
                return;
            }

            // user has indicated they covet the currently selected option
            // who are we to question their desires?
            var opt = layout.Option(oldAngleIndex).option;
            if (opt == null)
            {
                return;
            }
            var id = opt.DialogueOptionID;

            // can only select available options
            if (opt.IsAvailable)
            {
                SelectOption(id);
                return;
            }
            else if (AllowSelectingUnavailableOptions)
            {
                // unless we have specifically said we can
                SelectOption(id);
                return;
            }
        }

        /// <summary>
        /// Sets the <see cref="inputAngle"/> value for a joystick position, such as a gamepad.
        /// </summary>
        /// <remarks>
        /// If the parameter is not a normalised value, in the range of (-1, 1), this will not work as you might expect.
        /// </remarks>
        /// <param name="direction">The normalised direction of the joystick</param>
        public void SetAngleForJoystick(Vector2 direction)
        {
            inputAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            // Debug.Log($"was passed in ({direction.x},{direction.y}), resulting in {inputAngle}");
        }

        /// <summary>
        /// Works out based on the regions what region contains the angle.
        /// </summary>
        /// <param name="angle">The angle, in degrees, you want to find which region contains it</param>
        /// <param name="regions">The list of regions to check against the angle</param>
        /// <returns>The index of the region which holds the angle</returns>
        private int CalculateButtonIndex(float angle, OptionRegion[] regions)
        {
            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                var delta = Mathf.Abs(Mathf.DeltaAngle(angle, region.theta));

                if (delta <= (region.range / 2))
                {
                    // Debug.Log($"{angle} is within {region.theta} +- {region.range}. Resulting in {i}");
                    return i;
                }
            }

            return -1;
        }

        /// <inheritdoc />
        public override YarnTask OnDialogueCompleteAsync()
        {
            optionSelectedCompletionSource = null;

            if (presentationController != null)
            {
                presentationController.Dismiss();
            }

            if (layout != null)
            {
                layout.DestroyDialogueOptionViews();
            }

            regions = Array.Empty<OptionRegion>();
            return YarnTask.CompletedTask;
        }
    }
}
