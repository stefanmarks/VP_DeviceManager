using UnityEngine;
using UnityEngine.UI;

public class InfoUI : MonoBehaviour
{
	public Transform TrackedObject;

	public Text      TextUI;


	public void Start()
	{
		if (TextUI == null)
		{
			TextUI = GetComponentInChildren<Text>();
		}
		if (TextUI == null)
		{
			this.enabled = false;
		}

		m_formatString = TextUI.text;
		m_formatString = m_formatString.Replace("TrackedObjectName", TrackedObject.name);
	}

	
	public void Update()
	{
		if (TextUI != null) 
		{
			Vector3 pos = TrackedObject.localPosition;
			TextUI.text = string.Format(m_formatString, pos.x, pos.y, pos.z);
		}
	}

	protected string m_formatString;
}
