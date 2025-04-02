using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[AddComponentMenu("VP/IO Device")]
public class IO_Device : MonoBehaviour
{
	public InputActionProperty Input1;
	public bool Input1On;
	public InputActionProperty Input2;
	public bool Input2On;
	public InputActionProperty Input3;
	public bool Input3On;
	public InputActionProperty Input4;
	public bool Input4On;

	public InputActionProperty Output;


	public bool OutputOn;

	void Start()
	{
		Input1.action?.Enable();
		Input2.action?.Enable();
		Input3.action?.Enable();
		Input4.action?.Enable();
		Output.action?.Enable();
	}

	// Update is called once per frame
	void Update()
	{
		Input1On = Input1.action.inProgress;
		Input2On = Input2.action.inProgress;
		Input3On = Input3.action.inProgress;
		Input4On = Input4.action.inProgress;

		if (OutputOn)
		{
			var controls = Output.action?.controls;
			if (controls != null)
			{
				foreach (var control in controls)
				{
					var device = control.device;
					if (device is XRControllerWithRumble rumbleController)
					{
						rumbleController.SendImpulse(1.0f, 0.1f);
					}
				}
			}
			OutputOn = false;
		}
	}
}
