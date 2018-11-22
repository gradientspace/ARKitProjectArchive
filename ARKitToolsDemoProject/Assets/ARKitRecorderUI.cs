// Copyright (c) Ryan Schmidt - rms@gradientspace.com - twitter @rms80
// released under the MIT License (see LICENSE file)
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using gs;

public class ARKitRecorderUI : MonoBehaviour
{
	GameObject MainCanvas;

	Button record_play;
	GameObject options_panel;




	// Use this for initialization
	void Start()
	{
		MainCanvas = this.gameObject;

		record_play =
			MainCanvas.transform.Find("RecordPlayButton").gameObject.GetComponent<Button>();
		if (Application.isEditor) {
			record_play.onClick.AddListener(on_play);
		} else {
			record_play.GetComponentInChildren<Text>().text = "Record";
			record_play.onClick.AddListener(on_record);
		}


		options_panel = MainCanvas.transform.Find("OptionsPanel").gameObject;

		Toggle video_toggle =
			options_panel.transform.Find("RecordVideoToggle").gameObject.GetComponent<Toggle>();
		video_toggle.onValueChanged.AddListener(on_video_toggled);

		Button start =
			options_panel.transform.Find("StartButton").gameObject.GetComponent<Button>();
		start.onClick.AddListener(on_record_start);
		
		options_panel.SetActive(false);
	}


	void on_play() {
		ARKitSimulator.ActiveSimulator.Test();
	}
	void on_record() {
		if (ARKitStreamRecorder.IsRecording) {
			record_play.GetComponentInChildren<Text>().text = "Record";
			ARKitStreamRecorder.StopRecording();
		} else {
			options_panel.SetActive(true);
		}
	}


	void on_video_toggled(bool value) {
		ARKitStreamRecorder.EnableSaveVideoStream = value;
	}

	void on_record_start() {
		// do start
		options_panel.SetActive(false);
		record_play.GetComponentInChildren<Text>().text = "Stop";
		ARKitStreamRecorder.ErrorMessageF = Debug.Log;
		ARKitStreamRecorder.StartRecording();
	}


	// Update is called once per frame
	void Update()
	{
			
	}
}
