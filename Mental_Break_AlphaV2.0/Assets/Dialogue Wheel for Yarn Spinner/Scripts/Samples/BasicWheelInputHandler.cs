namespace Yarn.Unity.Addons.DialogueWheel.Samples
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Receives mouse and joystick movement, and updates a <see
    /// cref="WheelDialogueView"/> with angular information.
    /// </summary>
    /// <remarks>
    /// <para>This class is designed to be used as a sample during prototyping,
    /// and makes a number of assumptions that are unlikely to hold in a larger,
    /// more complicated game. This class uses either Unity's Input System or
    /// the Legacy Input System, has limited support for tuning and
    /// configuration, and does not work with Input Action Maps.
    /// </para>
    /// <para>
    /// Instead, we recommend that this class be used as an example of how to
    /// take user input and give it to a <see cref="WheelDialogueView"/> to
    /// control its current angle.
    /// </para>
    /// </summary>
    public class BasicWheelInputHandler : MonoBehaviour
    {
        /// <summary>
        /// Defines different sources of input for a <see cref="BasicWheelInputHandler"/> to
        /// respond to.
        /// </summary>
        public enum InputType
        {
            /// <summary>
            /// The <see cref="BasicWheelInputHandler"/> will respond to both
            /// mouse movement and controller joystick input.
            /// </summary>
            Auto,
            /// <summary>
            /// The <see cref="BasicWheelInputHandler"/> will respond to mouse movement.
            /// </summary>
            Mouse,
            /// <summary>
            /// The <see cref="BasicWheelInputHandler"/> will respond to controller joystick.
            /// </summary>
            Controller,
        }

        /// <summary>
        /// The type of input that this input handler responds to.
        /// </summary>
        [SerializeField] private InputType inputType;

        /// <summary>
        /// The <see cref="WheelDialogueView"/> that this class provides an
        /// angle to.
        /// </summary>
        [SerializeField] private WheelDialogueView view;

        /// <summary>
        /// The smoothing factor for mouse input.
        /// </summary>
        [UnityEngine.Serialization.FormerlySerializedAs("dampSpeed")]
        [SerializeField] private float mouseSmoothing = 0.25f;
        
        private Vector2 lastMousePosition;
        private Vector2 smoothDampVelocity;

        // Update is called once per frame
        void Update()
        {
            bool selected = false;
            if  (inputType == InputType.Auto)
            {
                var mouse = CheckMovementMouse();
                var stick = CheckMovementJoystick();

                if (stick == Vector2.zero)
                {
                    // the stick hasn't moved (or has sproinged back)
                    // but the mouse also hasn't moved
                    // so we don't want to just override the movement data
                    if (mouse != lastMousePosition)
                    {
                        var damp = Vector2.SmoothDamp(lastMousePosition, mouse, ref smoothDampVelocity, mouseSmoothing);
                        view.SetAngleForJoystick((damp - lastMousePosition).normalized);
                        lastMousePosition = mouse;
                    }
                }
                else
                {
                    view.SetAngleForJoystick(stick);
                }

                selected = PerformedSelectionMouse() || PerformedSelectionController();
            }
            else if (inputType == InputType.Mouse)
            {
                Vector2 move = CheckMovementMouse();

                if (move != lastMousePosition)
                {
                    var damp = Vector2.SmoothDamp(lastMousePosition, move, ref smoothDampVelocity, mouseSmoothing);
                    view.SetAngleForJoystick((damp - lastMousePosition).normalized);
                    lastMousePosition = move;
                }

                selected = PerformedSelectionMouse();
            }
            else
            {
                Vector2 move = CheckMovementJoystick();

                // if you just release the stick it sproings back to centre
                // which is very rarely ever gonna be what you want
                if (move != Vector2.zero)
                {
                    view.SetAngleForJoystick(move);
                }

                selected = PerformedSelectionController();
            }
            
            if (selected)
            {
                view.SelectCurrentlyHighlightedOption();
            }
        }

        private Vector2 CheckMovementJoystick()
        {
            Vector2 move;

#if USE_INPUTSYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad == null)
            {
                return Vector2.zero;
            }
            move = pad.leftStick.ReadValue();
#else
            var h = Input.GetAxis("Horizontal");
            var v = Input.GetAxis("Vertical");
            move = new Vector2(v, h);
#endif
            return move;
        }

        private Vector2 CheckMovementMouse()
        {
            Vector2 mouse;
#if USE_INPUTSYSTEM
            mouse = UnityEngine.InputSystem.Mouse.current.position.ReadUnprocessedValue();
#else
            mouse = Input.mousePosition;
#endif
            return mouse;
        }

        private bool PerformedSelectionMouse()
        {
#if USE_INPUTSYSTEM
            if (!UnityEngine.InputSystem.Mouse.current.leftButton.wasReleasedThisFrame)
            {
                return false;
            }
#else
            if (!Input.GetMouseButtonUp(0))
            {
                return false;
            }
#endif
            return true;
        }
        private bool PerformedSelectionController()
        {
#if USE_INPUTSYSTEM
            var pad = UnityEngine.InputSystem.Gamepad.current;
            if (pad == null)
            {
                return false;
            }
            
            if (!pad.buttonSouth.wasReleasedThisFrame)
            {
                return false;
            }
#else
            if (!Input.GetKeyUp("joystick button 1"))
            {
                return false;
            }
#endif
            return true;
        }
    }
}
