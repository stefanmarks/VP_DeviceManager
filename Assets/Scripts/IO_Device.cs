using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

public class IO_Device : MonoBehaviour
{
	public InputActionProperty Input1;
	public InputActionProperty Output;

	public bool InputOn;
	public bool OutputOn;

	void Start()
	{
		Input1.action?.Enable();
		Output.action?.Enable();
	}

	// Update is called once per frame
	void Update()
	{
		InputOn = Input1.action.inProgress;

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
