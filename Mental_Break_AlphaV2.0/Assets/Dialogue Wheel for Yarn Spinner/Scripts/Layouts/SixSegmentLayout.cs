namespace Yarn.Unity.Addons.DialogueWheel
{
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;
    using Yarn.Unity;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// A <see cref="DialogueWheelLayout"/> for laying out up to six options
    /// around a circle.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SixSegmentLayout"/> implements the common pattern used in
    /// many role-playing games where dialogue choices are arranged around a
    /// wheel in a hexagonal pattern. <see cref="SixSegmentLayout"/> allows each
    /// of the six positions around the circle to have a custom name, which can
    /// then be used in your Yarn scripts to control the position of each
    /// option.
    /// </para>
    /// <para>
    /// To set up a <see cref="SixSegmentLayout"/>,
    /// </para>
    /// <para>
    /// When the DialogueWheelLayout receives options to display, <see
    /// cref="SixSegmentLayout"/> reads those options to determine whether they
    /// have positioning tags. If all of them do, and there are no duplicated
    /// positions, then the options are mapped to specific points around the
    /// circle depending
    /// </remarks>
    public class SixSegmentLayout : DialogueWheelLayout
    {
        [SerializeField] private List<WheelOptionView> wheelOptions = new List<WheelOptionView>();

        // we send over to the dialogueview a list of regions
        // this list of regions is in the order of the dialogueOptions array
        // this list maps the indices of the regions sent over back to the actual regions
        private List<int> indexLocationMap = new List<int>();

        // the different hashtags for the positions
        [SerializeField] private string TopRightTag = "rt";
        [SerializeField] private string MiddleRightTag = "rm";
        [SerializeField] private string BottomRightTag = "rb";
        [SerializeField] private string TopLeftTag = "lt";
        [SerializeField] private string MiddleLeftTag = "lm";
        [SerializeField] private string BottomLeftTag = "lb";

        void Awake()
        {
            foreach (var opt in wheelOptions)
            {
                opt.gameObject.SetActive(false);
            }

            // registering two commands "set-opt" and "set-option-mask"
            // these allow basic control over the layout without needing full tagging
            var runner = GameObject.FindAnyObjectByType<DialogueRunner>();
            if (runner != null)
            {
                runner.AddCommandHandler<int, int>("set-opt", OptSide);
                runner.AddCommandHandler<bool, bool, bool, bool, bool, bool>("set-option-mask", SetMask);
            }
        }

        // just performing a basic sanity check on the position tags
        // we can't have duplicates and each tag must exist
        // if they are null/empty we put in the default
        void OnValidate()
        {
            if (string.IsNullOrEmpty(TopRightTag))
            {
                TopRightTag = "rt";
            }
            if (string.IsNullOrEmpty(MiddleRightTag))
            {
                MiddleRightTag = "rm";
            }
            if (string.IsNullOrEmpty(TopLeftTag))
            {
                TopLeftTag = "lt";
            }
            if (string.IsNullOrEmpty(MiddleLeftTag))
            {
                MiddleLeftTag = "lm";
            }
            if (string.IsNullOrEmpty(BottomLeftTag))
            {
                BottomLeftTag = "lb";
            }
            if (string.IsNullOrEmpty(BottomRightTag))
            {
                BottomRightTag = "rb";
            }
            HashSet<string> locations = new HashSet<string> { MiddleRightTag, TopRightTag, TopLeftTag, MiddleLeftTag, BottomLeftTag, BottomRightTag };
            if (locations.Count != 6)
            {
                Debug.LogError("Duplicate option location tags have been set!");
            }
        }

        // because we add some yarn commands we need to make sure we disable them
        // otherwise bad things will happen later
        void OnDestroy()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying)
            {
                return;
            }
#endif

            var runner = GameObject.FindFirstObjectByType<DialogueRunner>();
            runner.RemoveCommandHandler("set-opt");
            runner.RemoveCommandHandler("set-option-mask");
        }

        /// <summary>
        /// Configures the number of left and right options to be enabled in the next option group.
        /// </summary>
        /// <remarks>
        /// If set and turns out to be invalid for the number of options in the next group, will be ignored.
        ///</remarks>
        /// <param name="left">The number of options to be shown on the left side of the wheel</param>
        /// <param name="right">The number of options to be shown on the right shide of the wheel</param>
        public void OptSide(int left, int right)
        {
            // resetting the option mask to everything being off
            indexLocationMap.Clear();

            // doing some basic sanity checks
            if (left < 0 || left > 3)
            {
                Debug.LogWarning("Asked to layout an invalid number of left options, ignoring this and using autolayout.");
                return;
            }
            if (right < 0 || right > 3)
            {
                Debug.LogWarning("Asked to layout an invalid number of right options, ignoring this and using autolayout.");
                return;
            }

            // right is indices 0,1,5
            switch (right)
            {
                case 1:
                    {
                        indexLocationMap.Add(0);
                        break;
                    }
                case 2:
                    {
                        indexLocationMap.Add(1);
                        indexLocationMap.Add(5);
                        break;
                    }
                case 3:
                    {
                        indexLocationMap.Add(1);
                        indexLocationMap.Add(0);
                        indexLocationMap.Add(5);
                        break;
                    }
            }
            // left is 2,3,4
            switch (left)
            {
                case 1:
                    {
                        indexLocationMap.Add(3);
                        break;
                    }
                case 2:
                    {
                        indexLocationMap.Add(3);
                        indexLocationMap.Add(4);
                        break;
                    }
                case 3:
                    {
                        indexLocationMap.Add(2);
                        indexLocationMap.Add(3);
                        indexLocationMap.Add(4);
                        break;
                    }
            }
        }

        /// <summary>
        /// Enables or disables specific option positions on an individual basis.
        /// </summary>
        /// <remarks>
        /// If set and turns out to be invalid for the number of options in the next group, will be ignored.
        ///</remarks>
        /// <param name="rightMiddle">Should the middle right option be masked?</param>
        /// <param name="rightTop">Should the top right option be masked?</param>
        /// <param name="leftTop">Should the top left option be masked?</param>
        /// <param name="leftMiddle">Should the middle left option be masked?</param>
        /// <param name="leftBottom">Should the bottom left option be masked?</param>
        /// <param name="rightBottom">Should the bottom right option be masked?</param>
        public void SetMask(bool rightMiddle, bool rightTop, bool leftTop, bool leftMiddle, bool leftBottom, bool rightBottom)
        {
            // wow this is terrible
            indexLocationMap.Clear();
            if (rightMiddle)
            {
                indexLocationMap.Add(0);
            }
            if (rightTop)
            {
                indexLocationMap.Add(1);
            }
            if (leftTop)
            {
                indexLocationMap.Add(2);
            }
            if (leftMiddle)
            {
                indexLocationMap.Add(3);
            }
            if (leftBottom)
            {
                indexLocationMap.Add(4);
            }
            if (rightBottom)
            {
                indexLocationMap.Add(5);
            }
        }

        /// <inheritdoc />
        public override OptionRegion[] CreateDialogueRegions(DialogueOption[] dialogueOptions)
        {
            // there are two pathways through this
            // either we have set a manual location for each dialogue option
            // or we are doing normal masking
            // they are incompatible with each other and manual locations takes priority

            if (dialogueOptions.Length > 6)
            {
                Debug.LogError($"More than 6 options were provided. Showing only the first six.");
                var truncatedList = dialogueOptions.Take(6);
                return CreateDialogueRegions(truncatedList.ToArray());
            }

            // ok so first step is to collect all the manual locations
            // we are gonna assume the user has set this up right
            // and then check if that isn't the case afterwards
            List<string> locationTags = new List<string> { MiddleRightTag, TopRightTag, TopLeftTag, MiddleLeftTag, BottomLeftTag, BottomRightTag };
            var positions = new List<int>();
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var option = dialogueOptions[i];
                // do we have any metadata on the option?
                if (option.Line.Metadata == null)
                {
                    break;
                }

                var intersect = option.Line.Metadata.Intersect(locationTags);
                // do we have any of the location tags in the metadata?
                if (!intersect.Any())
                {
                    break;
                }

                // do we only have *one* of the location tags?
                if (intersect.Count() != 1)
                {
                    Debug.LogWarning($"Option {option.Line.TextID} has asked to be placed in two locations simultaneously. This is not possible. Falling back to automatic layout.");
                    positions.Clear();
                    break;
                }

                var loc = locationTags.IndexOf(intersect.First());
                positions.Add(loc);
            }

            // some (hopefully all) of the options have had their manual location set
            if (positions.Count > 0)
            {
                // while we have some manually tagged options, not enough or we have duplicates
                var uniques = positions.Intersect(positions).Count();
                if (uniques != dialogueOptions.Length)
                {
                    Debug.LogWarning("Not all options have a unique location set. Falling back to automatic layout.");

                    // wiping the positions so that auto layout can occur
                    indexLocationMap.Clear();
                    positions.Clear();
                    positions.AddRange(Enumerable.Range(0, dialogueOptions.Length));
                }
                else
                {
                    // we have a valid manual layout
                    // but we might also have a mask
                    // this conflicts so we say we are ignoring the mask
                    if (indexLocationMap.Count > 0)
                    {
                        Debug.LogWarning("Asked to perform a manual layout but an option layout has also been set, ignoring the option layout.");
                        indexLocationMap.Clear();
                    }
                }
            }
            else
            {
                // we didn't end up with any manual layout positions
                // next step is to then see if there is a mask
                // and if there is, is it valid for these options

                // there is a mask, time to see if it is valid
                if (indexLocationMap.Count > 0)
                {
                    if (indexLocationMap.Count == dialogueOptions.Length)
                    {
                        // mask is valid
                        // using it for layout
                        positions = indexLocationMap;
                    }
                    else
                    {
                        // the mask set is invalid for the number of options we have
                        // falling back to autolayout
                        Debug.LogWarning("A layout for the options has been set but it does not match the number of options. Falling back to automatic layout.");
                        indexLocationMap.Clear();
                        positions.AddRange(Enumerable.Range(0, dialogueOptions.Length));
                    }
                }
                else
                {
                    // we have no mask, falling back to autolayout
                    positions.AddRange(Enumerable.Range(0, dialogueOptions.Length));
                }
            }

            // ok so at this point we have all the locations determined
            // either by the masking, manually positioned, or fallback
            // next step is to turn on the different option gameobjects
            var r = new OptionRegion[dialogueOptions.Length];
            for (int i = 0; i < dialogueOptions.Length; i++)
            {
                var option = dialogueOptions[i];
                var position = positions[i];
                var wheel = wheelOptions[position];

                wheel.option = option;
                wheel.Configure();
                wheel.gameObject.SetActive(true);

                r[i] = regions[position];
            }
            // saving these positions for later use
            indexLocationMap = positions;
            return r;
        }

        /// <inheritdoc />
        public override void DestroyDialogueOptionViews()
        {
            // just turning off all the options
            foreach (var opt in wheelOptions)
            {
                opt.gameObject.SetActive(false);
            }
            // and clearing the mask
            indexLocationMap.Clear();
        }

        /// <inheritdoc />
        public override WheelOptionView Option(int index)
        {
            // ok so we get given this index which is the index of the regions inside the location map
            // which means the value inside of indexLocationMap is the index of the option itself
            return wheelOptions[indexLocationMap[index]];
        }

        public override IEnumerable<WheelOptionView> GetAllOptionViews()
        {
            return wheelOptions;
        }
    }
}
