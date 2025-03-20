using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.XR;
using UnityEngine.Scripting;
using UnityEngine.XR.OpenXR.Input;

#if UNITY_EDITOR
using UnityEditor;
#endif
 
#if USE_INPUT_SYSTEM_POSE_CONTROL // Scripting Define Symbol added by using OpenXR Plugin 1.6.0.
using PoseControl = UnityEngine.InputSystem.XR.PoseControl;
#else
using PoseControl = UnityEngine.XR.OpenXR.Input.PoseControl;
#endif

/// OpenXR HTC vive tracker interaction specification
///     <see href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_HTCX_vive_tracker_interaction"/>
/// original HTC Vive Tracker Interaction Profile with only device Pose
///     <see href="https://discussions.unity.com/t/openxr-and-openvr-together/841675/21"/>
/// Vive's official OpenXR interaction profile for the Ultimate Tracker
///     <see href="https://github.com/ViveSoftware/VIVE-OpenXR/blob/5ac252bf2edfcbc04d952e183bfaa49d35534b2e/com.htc.upm.vive.openxr/Runtime/Features/Tracker/XR/Scripts/ViveXRTracker.cs"/>

namespace UnityEngine.XR.OpenXR.Features.Interactions
{
	/// <summary>
	/// This <see cref="OpenXRInteractionFeature"/> enables the use of HTC Vive Trackers interaction profiles in OpenXR.
	/// </summary>
#if UNITY_EDITOR
	[UnityEditor.XR.OpenXR.Features.OpenXRFeature(
		UiName = "HTC Vive Tracker Profile",
		BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.WSA },
		Company = "MASSIVE",
		Desc = "Allows for mapping input to the HTC Vive Tracker interaction profile.",
		DocumentationLink = Constants.k_DocumentationManualURL,
		OpenxrExtensionStrings = HTCViveTrackerProfile.extensionName,
		Version = "0.0.1",
		Category = UnityEditor.XR.OpenXR.Features.FeatureCategory.Interaction,
		FeatureId = featureId)]
#endif

	public class HTCViveTrackerProfile : OpenXRInteractionFeature
	{
		/// <summary>
		/// The name of the OpenXR extension that supports the Vive Tracker
		/// </summary>
		public const string extensionName = "XR_HTCX_vive_tracker_interaction";
		
		/// <summary>
		/// The feature id string. This is used to give the feature a well known id for reference
		/// </summary>
		public const string featureId = "com.massive.openxr.feature.input.htcvivetracker";
 
		/// <summary>
		/// The interaction profile string used to reference the <a href="https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#:~:text=in%20this%20case.-,VIVE%20Tracker%20interaction%20profile,-Interaction%20profile%20path">HTC Vive Tracker</a>.
		/// </summary>
		public const string profile = "/interaction_profiles/htc/vive_tracker_htcx";
 
		/// <summary>
		/// Localized name of the profile.
		/// </summary>
		private const string kDeviceLocalizedName = "HTC Vive Tracker OpenXR";

		/// <summary>
		/// Localized name of the layout.
		/// </summary>
		private const string kLayoutName = "ViveTracker";

		/// <summary>
		/// OpenXR user path definitions for XR_HTCX_vive_tracker_interaction
		/// </summary>
		public static class HTCViveTrackerUserPaths
		{
			/// <summary>
			/// Path for user left foot
			/// </summary>
			public const string leftFoot = "/user/vive_tracker_htcx/role/left_foot";
 
			/// <summary>
			/// Path for user roght foot
			/// </summary>
			public const string rightFoot = "/user/vive_tracker_htcx/role/right_foot";
 
			/// <summary>
			/// Path for user left shoulder
			/// </summary>
			public const string leftShoulder = "/user/vive_tracker_htcx/role/left_shoulder";
 
			/// <summary>
			/// Path for user right shoulder
			/// </summary>
			public const string rightShoulder = "/user/vive_tracker_htcx/role/right_shoulder";
 
			/// <summary>
			/// Path for user left elbow
			/// </summary>
			public const string leftElbow = "/user/vive_tracker_htcx/role/left_elbow";
 
			/// <summary>
			/// Path for user right elbow
			/// </summary>
			public const string rightElbow = "/user/vive_tracker_htcx/role/right_elbow";
 
			/// <summary>
			/// Path for user left knee
			/// </summary>
			public const string leftKnee = "/user/vive_tracker_htcx/role/left_knee";
 
			/// <summary>
			/// Path for user right knee
			/// </summary>
			public const string rightKnee = "/user/vive_tracker_htcx/role/right_knee";
 
			/// <summary>
			/// Path for user waist
			/// </summary>
			public const string waist = "/user/vive_tracker_htcx/role/waist";
 
			/// <summary>
			/// Path for user chest
			/// </summary>
			public const string chest = "/user/vive_tracker_htcx/role/chest";
 
			/// <summary>
			/// Path for user custom camera
			/// </summary>
			public const string camera = "/user/vive_tracker_htcx/role/camera";
 
			/// <summary>
			/// Path for user keyboard
			/// </summary>
			public const string keyboard = "/user/vive_tracker_htcx/role/keyboard";
		}
 
		/// <summary>
		/// OpenXR component path definitions for the tracker.
		/// </summary>
		public static class HTCViveTrackerComponentPaths
		{
			// type PoseControl
			public const string devicepose = "/input/grip/pose";

			// type ButtonControl
			public const string system   = "/input/system/click"; // may not be available for application use
			public const string menu     = "/input/menu/click";
			public const string grip     = "/input/squeeze/click";
			public const string trigger  = "/input/trigger/click";
			public const string pad      = "/input/trackpad/click";
			public const string padTouch = "/input/trackpad/touch";

			// type AxisControl
			public const string triggerValue = "/input/trigger/value";
			public const string padXValue    = "/input/trackpad/x";
			public const string padYValue    = "/input/trackpad/y";

			//type HapticControl
			public const string haptic = "/output/haptic";
		}
 
		/// <summary>
		/// An Input System device based off the <a href="https://registry.khronos.org/OpenXR/specs/1.0/html/xrspec.html#XR_HTCX_vive_tracker_interaction">HTC Vive Tracker OpenXR specifications
		/// </summary>
		[Preserve, InputControlLayout(displayName = "HTC Vive Tracker (OpenXR)", commonUsages = new[] { "Left Foot", "Right Foot", "Left Shoulder", "Right Shoulder", "Left Elbow", "Right Elbow", "Left Knee", "Right Knee", "Waist", "Chest", "Camera", "Keyboard" }, isGenericTypeOfDevice = true)]
		public class HTCViveTrackerOpenXR : XRControllerWithRumble
		{
			#region Pose
			/// <summary>
			/// device pose. Contains isTracked, trackingState, position, rotation, velocity, and angularVelocity 
			/// <see cref="HTCViveTrackerComponentPaths.devicepose"/>
			/// </summary>
			[Preserve, InputControl(offset = 0, aliases = new[] { "device", "entityPose" }, usage = "Device", noisy = true)]
			public PoseControl devicePose { get; private set; }

			/// <summary>
			/// if data is valid, equivalent to devicePose/isTracked
			/// necessary for compatibility with XRSDK layouts
			/// </summary>
			[Preserve, InputControl(offset = 60, usage = "IsTracked")]
			new public ButtonControl isTracked { get; private set; }

			/// <summary>
			/// represents what data is valid, equivalent to devicePose/trackingState
			/// necessary for compatibility with XRSDK layouts
			/// </summary>
			[Preserve, InputControl(offset = 64, usage = "TrackingState")]
			new public IntegerControl trackingState { get; private set; }

			/// <summary>
			/// A [Vector3Control](xref:UnityEngine.InputSystem.Controls.Vector3Control) required for back compatibility with the XRSDK layouts. This is the device position. For the Oculus Touch device, this is both the grip and the pointer position. This value is equivalent to mapping devicePose/position.
			/// </summary>
			[Preserve, InputControl(offset = 8, alias = "gripPosition", noisy = true)]
			new public Vector3Control devicePosition { get; private set; }

			/// <summary>
			/// A [QuaternionControl](xref:UnityEngine.InputSystem.Controls.QuaternionControl) required for backwards compatibility with the XRSDK layouts. This is the device orientation. For the Oculus Touch device, this is both the grip and the pointer rotation. This value is equivalent to mapping devicePose/rotation.
			/// </summary>
			[Preserve, InputControl(offset = 20, alias = "gripOrientation", noisy = true)]
			new public QuaternionControl deviceRotation { get; private set; }
			#endregion

			#region Boolean Inputs
			/// <summary>
			/// System button on top of tracker. May not be available for application use
			/// <see cref="HTCViveTrackerComponentPaths.system"/>
			/// </summary>
			[Preserve, InputControl(alias = "systemButton", usage = "SystemButton")]
			public ButtonControl system { get; private set; }

			/// <summary>
			/// Menu button. Accessed on vive tracker 3.0 via USB or by shorting pins 2 and 6
			/// <see cref="HTCViveTrackerComponentPaths.menu"/>
			/// </summary>
			[Preserve, InputControl(alias = "menuButton", usage = "MenuButton")]
			public ButtonControl menu { get; private set; }

			/// <summary>
			/// Grip button. Accessed on vive tracker 3.0 via USB or by shorting pins 2 and 3
			/// <see cref="HTCViveTrackerComponentPaths.grip"/>
			/// </summary>
			[Preserve, InputControl(alias = "gripButton", usage = "GripButton")]
			public ButtonControl grip { get; private set; }

			/// <summary>
			/// Trigger button. Accessed on vive tracker 3.0 via USB or by shorting pins 2 and 4
			/// <see cref="HTCViveTrackerComponentPaths.trigger"/>
			/// </summary>
			[Preserve, InputControl(alias = "triggerButton", usage = "TriggerButton")]
			public ButtonControl trigger { get; private set; }

			/// <summary>
			/// Trackpad button. Accessed on vive tracker 3.0 via USB or by shorting pins 2 and 5
			/// <see cref="HTCViveTrackerComponentPaths.pad"/>
			/// </summary>
			[Preserve, InputControl(alias = "trackpadButton", usage = "TrackpadButton")]
			public ButtonControl pad { get; private set; }
/*
			/// <summary>
			/// Trackpad touch. Only accessible on vive tracker 3.0 via USB
			/// <see cref="TrackerComponentPaths.padTouch"/>
			/// </summary>
			[Preserve, InputControl(alias = "trackpadTouch", usage = "TrackpadTouch")]
			public ButtonControl padTouch { get; private set; }
*/
			#endregion

			#region Float Inputs
			/// <summary>
			/// Trigger pull analog value. Only accessible on vive tracker 3.0 via USB
			/// <see cref="TrackerComponentPaths.triggerValue"/>
			/// </summary>
			[Preserve, InputControl(alias = "triggerValue", usage = "TriggerValue")]
			public AxisControl triggerValue { get; private set; }

			/// <summary>
			/// Trackpad X/horizontal analog value. Only accessible on vive tracker 3.0 via USB
			/// <see cref="HTCViveTrackerComponentPaths.padXValue"/>
			/// </summary>
			[Preserve, InputControl(alias = "trackpadXValue", usage = "TrackpadXValue")]
			public AxisControl padXValue { get; private set; }

			/// <summary>
			/// Trackpad Y/vertical analog value. Only accessible on vive tracker 3.0 via USB
			/// <see cref="HTCViveTrackerComponentPaths.padYValue"/>
			/// </summary>
			[Preserve, InputControl(alias = "trackpadYValue", usage = "TrackpadYValue")]
			public AxisControl padYValue { get; private set; }
			#endregion

			#region Device Outputs
			/// <summary>
			/// Haptic outputs. Accessed on vive tracker 3.0 by pogo pin 1. Untested via USB
			/// <see cref="HTCViveTrackerComponentPaths.haptic"/>
			/// </summary>
			[Preserve, InputControl(usage = "Haptic")]
			public HapticControl haptic { get; private set; }
			#endregion

			/// <inheritdoc cref="OpenXRDevice"/>
			protected override void FinishSetup()
			{
				base.FinishSetup();

				devicePose     = GetChildControl<PoseControl>("devicePose");
				isTracked      = GetChildControl<ButtonControl>("isTracked");
				trackingState  = GetChildControl<IntegerControl>("trackingState");
				devicePosition = GetChildControl<Vector3Control>("devicePosition");
				deviceRotation = GetChildControl<QuaternionControl>("deviceRotation");

				system   = GetChildControl<ButtonControl>("system");
				menu     = GetChildControl<ButtonControl>("menu");
				grip     = GetChildControl<ButtonControl>("grip");
				trigger  = GetChildControl<ButtonControl>("trigger");
				pad      = GetChildControl<ButtonControl>("pad");
/*
				padTouch = GetChildControl<ButtonControl>("padTouch");

				triggerValue = GetChildControl<AxisControl>("triggerValue");
				padXValue = GetChildControl<AxisControl>("padXValue");
				padYValue = GetChildControl<AxisControl>("padYValue");
*/
				haptic = GetChildControl<HapticControl>("haptic");

				var deviceDescriptor = XRDeviceDescriptor.FromJson(description.capabilities);

				if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftFoot) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Left Foot");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightFoot) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Right Foot");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftShoulder) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Left Shoulder");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightShoulder) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Right Shoulder");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftElbow) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Left Elbow");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightElbow) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Right Elbow");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftKnee) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Left Knee");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightKnee) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Right Knee");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerWaist) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Waist");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerChest) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Chest");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerCamera) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Camera");
				else if ((deviceDescriptor.characteristics & (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerKeyboard) != 0)
					InputSystem.InputSystem.SetDeviceUsage(this, "Keyboard");
				else {
					Debug.Log($"No tracker role could be found that matches device '{base.displayName}'");
				}

				// Debug.Log($"Device '{base.displayName}' added");
			}
		}

		/// <summary>
		/// Registers the <see cref="ViveTracker"/> layout with the Input System.
		/// </summary>
		protected override void RegisterDeviceLayout()
		{
			//InputSystem.InputSystem.RegisterLayout<XRController>();
			//InputSystem.InputSystem.RegisterLayout<XRControllerWithRumble>();
			InputSystem.InputSystem.RegisterLayout(typeof(HTCViveTrackerOpenXR),
						matches: new InputDeviceMatcher()
						.WithInterface(XRUtilities.InterfaceMatchAnyVersion)
						.WithProduct(kDeviceLocalizedName));
		}

		/// <summary>
		/// Removes the <see cref="ViveTracker"/> layout from the Input System.
		/// </summary>
		protected override void UnregisterDeviceLayout()
		{
			InputSystem.InputSystem.RemoveLayout(nameof(HTCViveTrackerOpenXR));
			//InputSystem.InputSystem.RemoveLayout(nameof(XRControllerWithRumble));
			//InputSystem.InputSystem.RemoveLayout(nameof(XRController));
		}


		/// <summary>
		/// Return interaction profile type. XRViveTracker profile is Device type. 	
		/// </summary>
		/// <returns>Interaction profile type.</returns> 	
		protected override InteractionProfileType GetInteractionProfileType()	
		{
			return typeof(HTCViveTrackerOpenXR).IsSubclassOf(typeof(XRController)) ? InteractionProfileType.XRController : InteractionProfileType.Device;	
		}

		/// <summary>
		/// Return device layer out string used for registering device VIVEFocus3Controller in InputSystem. 	
		/// </summary>
		/// <returns>Device layout string.</returns> 	
		protected override string GetDeviceLayoutName()	
		{	
			return kLayoutName;	
		}	

		/// <summary>
		/// A set of bit flags describing XR.InputDevice characteristics
		/// </summary>
		[Flags]
		public enum InputDeviceTrackerCharacteristics : uint
		{
			TrackerLeftFoot      = 0x1000u,
			TrackerRightFoot     = 0x2000u,
			TrackerLeftShoulder  = 0x4000u,
			TrackerRightShoulder = 0x8000u,
			TrackerLeftElbow     = 0x10000u,
			TrackerRightElbow    = 0x20000u,
			TrackerLeftKnee      = 0x40000u,
			TrackerRightKnee     = 0x80000u,
			TrackerWaist         = 0x100000u,
			TrackerChest         = 0x200000u,
			TrackerCamera        = 0x400000u,
			TrackerKeyboard      = 0x800000u
		}

		/// <inheritdoc/>
		protected override void RegisterActionMapsWithRuntime()
		{
			ActionMapConfig actionMap = new ActionMapConfig()
			{
				name = "htcvivetracker",
				localizedName = kDeviceLocalizedName,
				desiredInteractionProfile = profile,
				manufacturer = "HTC",
				serialNumber = "",
				deviceInfos = new List<DeviceConfig>()
				{
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftFoot),
						userPath = HTCViveTrackerUserPaths.leftFoot
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightFoot),
						userPath = HTCViveTrackerUserPaths.rightFoot
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftShoulder),
						userPath = HTCViveTrackerUserPaths.leftShoulder
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightShoulder),
						userPath = HTCViveTrackerUserPaths.rightShoulder
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftElbow),
						userPath = HTCViveTrackerUserPaths.leftElbow
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightElbow),
						userPath = HTCViveTrackerUserPaths.rightElbow
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerLeftKnee),
						userPath = HTCViveTrackerUserPaths.leftKnee
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerRightKnee),
						userPath = HTCViveTrackerUserPaths.rightKnee
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerWaist),
						userPath = HTCViveTrackerUserPaths.waist
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerChest),
						userPath = HTCViveTrackerUserPaths.chest
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerCamera),
						userPath = HTCViveTrackerUserPaths.camera
					},
					new DeviceConfig()
					{
						characteristics = (InputDeviceCharacteristics.TrackedDevice | InputDeviceCharacteristics.Controller | (InputDeviceCharacteristics)InputDeviceTrackerCharacteristics.TrackerKeyboard),
						userPath = HTCViveTrackerUserPaths.keyboard
					}
				},
				actions = new List<ActionConfig>()
				{
					 new ActionConfig()
					{
						name = "devicePose",
						localizedName = "Grip Pose",
						type = ActionType.Pose,
						usages = new List<string>() { "Device" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.devicepose,
								interactionProfileName = profile,
							}
						}
					},
					// System button
					new ActionConfig()
					{
						name = "system",
						localizedName = "System",
						type = ActionType.Binary,
						usages = new List<string>() { "SystemButton" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.system,
								interactionProfileName = profile,
							}
						}
					},
					// Menu button
					new ActionConfig()
					{
						name = "menu",
						localizedName = "Menu",
						type = ActionType.Binary,
						usages = new List<string>() { "MenuButton" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.menu,
								interactionProfileName = profile,
							}
						}
					},
					// Grip button
					new ActionConfig()
					{
						name = "grip",
						localizedName = "Grip",
						type = ActionType.Binary,
						usages = new List<string>() { "GripButton" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.grip,
								interactionProfileName = profile,
							}
						}
					},
					// Trigger button
					new ActionConfig()
					{
						name = "trigger",
						localizedName = "Trigger",
						type = ActionType.Binary,
						usages = new List<string>()  { "TriggerButton" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.trigger,
								interactionProfileName = profile,
							}
						}
					},
					// Trackpad button
					new ActionConfig()
					{
						name = "pad",
						localizedName = "Pad",
						type = ActionType.Binary,
						usages = new List<string>() { "TrackpadButton" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.pad,
								interactionProfileName = profile,
							}
						}
					},
/*
					// Trackpad touch
					new ActionConfig()
					{
						name = "padTouch",
						localizedName = "PadTouch",
						type = ActionType.Binary,
						usages = new List<string>() { "TrackpadTouch" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = TrackerComponentPaths.padTouch,
								interactionProfileName = profile,
							}
						}
					},
					// Trigger pull analog value
					new ActionConfig()
					{
						name = "triggerValue",
						localizedName = "TriggerValue",
						type = ActionType.Axis1D,
						usages = new List<string>() { "TrackpadValue" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = TrackerComponentPaths.triggerValue,
								interactionProfileName = profile,
							}
						}
					},
					// Trackpad X/horizontal analog value
					new ActionConfig()
					{
						name = "padXValue",
						localizedName = "Trackpad X Value",
						type = ActionType.Axis1D,
						usages = new List<string>() { "TrackpadXValue" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = TrackerComponentPaths.padXValue,
								interactionProfileName = profile,
							}
						}
					},
					// Trackpad Y/vertical analog value
					new ActionConfig()
					{
						name = "padYValue",
						localizedName = "Trackpad Y Value",
						type = ActionType.Axis1D,
						usages = new List<string>() { "TrackpadYValue" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = TrackerComponentPaths.padYValue,
								interactionProfileName = profile,
							}
						}
					},
*/
					// Haptic output
					new ActionConfig()
					{
						name = "haptic",
						localizedName = "Haptic Output",
						type = ActionType.Vibrate,
						usages = new List<string>() { "Haptic" },
						bindings = new List<ActionBinding>()
						{
							new ActionBinding()
							{
								interactionPath = HTCViveTrackerComponentPaths.haptic,
								interactionProfileName = profile,
							}
						}
					}
				}
			};
 
			AddActionMap(actionMap);
		}
 
		protected override bool OnInstanceCreate(ulong xrInstance)
		{
			bool res = base.OnInstanceCreate(xrInstance);

			string name = kDeviceLocalizedName + " Extension ";
			if (OpenXRRuntime.IsExtensionEnabled(extensionName)) {
				Debug.Log(name + "Enabled");
			}
			else {
				Debug.LogWarning(name + "Not Enabled");
			}

			return res;
		}
	}
}
