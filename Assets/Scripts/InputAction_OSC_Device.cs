using SentienceLab.OSC;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

[AddComponentMenu("VP/InputAction OSC Device")]
public class InputAction_Device : MonoBehaviour, IOSCVariableContainer
{
	public string OSC_Prefix            = "/io_device";
	public float  MinimumUpdateInterval = 1.0f;

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


	public void Start()
	{
		Input1.action?.Enable();
		Input2.action?.Enable();
		Input3.action?.Enable();
		Input4.action?.Enable();
		Output.action?.Enable();

		m_input1 = new OSC_BoolVariable(OSC_Prefix + "/input1");
		m_input2 = new OSC_BoolVariable(OSC_Prefix + "/input2");
		m_input3 = new OSC_BoolVariable(OSC_Prefix + "/input3");
		m_input4 = new OSC_BoolVariable(OSC_Prefix + "/input4");
		m_output = new OSC_BoolVariable(OSC_Prefix + "/output");
		
		m_nextIO_Update = 0;
	}


	public void Update()
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

		m_nextIO_Update -= Time.deltaTime;
		bool doUpdate = m_nextIO_Update <= 0;

		if (m_input1.Value != Input1On) { m_input1.Value = Input1On; doUpdate = true; }
		if (m_input2.Value != Input2On) { m_input2.Value = Input2On; doUpdate = true; }
		if (m_input3.Value != Input3On) { m_input3.Value = Input3On; doUpdate = true; }
		if (m_input4.Value != Input4On) { m_input4.Value = Input4On; doUpdate = true; }

		if (doUpdate)
		{
			m_input1.SendUpdate();
			m_input2.SendUpdate();
			m_input3.SendUpdate();
			m_input4.SendUpdate();
			m_nextIO_Update = MinimumUpdateInterval;
		}
	}

	public List<OSC_Variable> GetOSC_Variables()
	{
		return new List<OSC_Variable> { m_output };
	}


	protected OSC_BoolVariable m_input1, m_input2, m_input3, m_input4;
	protected OSC_BoolVariable m_output;
	protected double           m_nextIO_Update;
}
