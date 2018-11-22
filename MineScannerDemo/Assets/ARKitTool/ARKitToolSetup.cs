using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.iOS;
using System.IO;

using g3;
using f3;
using gs;


public static class gsGlobal
{
	public static bool EnableTracking = false;
	public static bool ResetPending = false;


	public static float GroundY = 0.0f;

	public static float GridSize = 0.02f;
	public static int MinSampleCount = 50;

	public static bool ShowDots = true;
	public static bool ShowMesh = true;

	public static Action<string> TextErrorHandlerF = (str) => { };
}


public class ARKitToolSetup : MonoBehaviour 
{
	public GameObject ParticleGridVizGO;  // used for simulator


	Canvas mainCanvas;

	Button trackButton;

	Button resetButton;
	int reset_counter = 0;

	Button sizeButton;
	enum SizeMode {
		cm_1 = 0,
		cm_2 = 1,
		cm_5 = 2,
		cm_10 = 3,
		cm_25 = 4,
		cm_50 = 5,
		cm_100 = 6,

		size_last = 7
	}
	SizeMode curSize = SizeMode.cm_2;

	Dropdown viewDropDown;

	Text debugText;


	Button optionsButton;

	GameObject optionsPanel;
	Dropdown qualityDropDown;
	bool enable_stream_recording = false;

	// Use this for initialization
	void Start () {

		FPlatform.InitializeMainThreadID();  // for DebugUtil.EmitDebug()

		mainCanvas = GameObject.Find("UICanvas").GetComponent<Canvas>();

		trackButton = GameObject.Find("TrackButton").GetComponent<Button>();
		trackButton.onClick.AddListener(track_click);

		resetButton = GameObject.Find("ResetButton").GetComponent<Button>();
		resetButton.onClick.AddListener(reset_click);

		sizeButton = GameObject.Find("SizeButton").GetComponent<Button>();
		sizeButton.onClick.AddListener(size_click);

		curSize = SizeMode.cm_2;
		gsGlobal.GridSize = 0.02f;
		sizeButton.GetComponentInChildren<Text>().text = "2cm";

		viewDropDown = GameObject.Find("ViewModeDropDown").GetComponent<Dropdown>();
		viewDropDown.onValueChanged.AddListener(view_changed);

		debugText = GameObject.Find("DebugText").GetComponent<Text>();
		debugText.text = "";
		GameObject.Find("DebugTextCloseButton").GetComponent<Button>().onClick.AddListener(hide_error_text);
		debugText.gameObject.SetVisible(false);
		gsGlobal.TextErrorHandlerF = set_error_text;


		GameObject simulateButtonGO = UnityUtil.FindGameObjectByName("SimulateButton");
		if ( FPlatform.InUnityEditor() ) {
			simulateButtonGO.SetVisible(true);
			simulateButtonGO.GetComponent<Button>().onClick.AddListener(simulate_ar);
		}

		optionsButton = GameObject.Find("OptionsButton").GetComponent<Button>();
		optionsButton.onClick.AddListener(options_click);

		UnityUtil.FindGameObjectByName("OptionSaveARStream").GetComponent<Toggle>().
		         onValueChanged.AddListener(option_savearstream_changed);
		UnityUtil.FindGameObjectByName("OptionSaveVideoStream").GetComponent<Toggle>().
				 onValueChanged.AddListener(option_save_video_stream_changed);
		qualityDropDown = UnityUtil.FindGameObjectByName("OptionQualityDropDown").GetComponent<Dropdown>();
		qualityDropDown.onValueChanged.AddListener(on_quality_changed);

		optionsPanel = UnityUtil.FindGameObjectByName("OptionsPanel");
		optionsPanel.SetVisible(false);

	}


	void track_click() {
		if (gsGlobal.EnableTracking) {
			gsGlobal.EnableTracking = false;
			trackButton.GetComponentInChildren<Text>().text = "Track";

			if ( ARKitStreamRecorder.IsRecording )
				ARKitStreamRecorder.StopRecording();
		} else {
			gsGlobal.EnableTracking = true;
			trackButton.GetComponentInChildren<Text>().text = "Stop";

			if ( enable_stream_recording && ARKitStreamRecorder.IsRecording == false )
				ARKitStreamRecorder.StartRecording();
		}
	}

	void reset_click() {
		gsGlobal.ResetPending = true;
		reset_counter = 2;
	}



	void size_click() {
		curSize = (SizeMode)(((int)curSize + 1) % (int)SizeMode.size_last);
		switch (curSize) {
			case SizeMode.cm_1: 
				gsGlobal.GridSize = 0.01f; 
				sizeButton.GetComponentInChildren<Text>().text = "1cm";
				break;
			default:
			case SizeMode.cm_2: 
				gsGlobal.GridSize = 0.02f;
				sizeButton.GetComponentInChildren<Text>().text = "2cm";
				break;
			case SizeMode.cm_5: 
				gsGlobal.GridSize = 0.05f;
				sizeButton.GetComponentInChildren<Text>().text = "5cm";
				break;
			case SizeMode.cm_10: 
				gsGlobal.GridSize = 0.1f;
				sizeButton.GetComponentInChildren<Text>().text = "10cm";
				break;
			case SizeMode.cm_25: 
				gsGlobal.GridSize = 0.25f;
				sizeButton.GetComponentInChildren<Text>().text = "25cm";
				break;
			case SizeMode.cm_50: 
				gsGlobal.GridSize = 0.5f;
				sizeButton.GetComponentInChildren<Text>().text = "50cm";
				break;
			case SizeMode.cm_100: 
				gsGlobal.GridSize = 1.0f;
				sizeButton.GetComponentInChildren<Text>().text = "1m";
				break;
				
		}

		reset_click();
	}



	void view_changed(int newValue) {
		if (viewDropDown.value == 0 ) {
			gsGlobal.ShowDots = gsGlobal.ShowMesh = true;
		} else if ( viewDropDown.value == 1 ) {
			gsGlobal.ShowDots = false; gsGlobal.ShowMesh = true;
		} else if (viewDropDown.value == 2) {
			gsGlobal.ShowDots = true; gsGlobal.ShowMesh = false;
		} else if (viewDropDown.value == 3) {
			gsGlobal.ShowDots = gsGlobal.ShowMesh = false;
		}
	}



	void set_error_text(string s) {
		debugText.text = s;
		if (debugText.gameObject.IsVisible() == false) {
			debugText.gameObject.SetVisible(true);
			debugText.gameObject.SetLocalPosition(
				debugText.gameObject.GetLocalPosition() + 1000 * Vector3f.AxisY);
		}
	}
	void hide_error_text() {
		debugText.gameObject.SetLocalPosition(
			debugText.gameObject.GetLocalPosition() - 1000 * Vector3f.AxisY);
		debugText.gameObject.SetVisible(false);		
	}




	void options_click() {
		if (optionsPanel.IsVisible())
			optionsPanel.SetVisible(false);
		else
			optionsPanel.SetVisible(true);
	}


	void on_quality_changed(int to) {
		if ( to == 0 ) {
			gsGlobal.MinSampleCount = 10;
		} else if ( to == 1 ) {
			gsGlobal.MinSampleCount = 25;
		} else if ( to == 2 ) {
			gsGlobal.MinSampleCount = 50;
		} else if ( to == 3 ) {
			gsGlobal.MinSampleCount = 100;
		}
	}


	void option_savearstream_changed(bool to) {
		enable_stream_recording = to;
	}
	void option_save_video_stream_changed(bool to)
	{
		ARKitStreamRecorder.EnableSaveVideoStream = to;
	}





	gs.ARKitStreamPlayback FakeStream = null;
	string current_filename = null;
	GameObject samplesParent;

	void simulate_ar() {
		if (FakeStream != null)
			FakeStream.Stop(true);

		//string sFilename = "/Users/rms/scratch/ARStream_15-7-2017-20-23-23.txt";
		// 5mb
		//string sFilename = "/Users/rms/scratch/ARStream_15-7-2017-18-21-12.txt";
		string sFilename = "/Users/rms/scratch/ARStream_16-7-2017-22-25-2.txt";
		// w/ video
		//string sFilename = "/Users/rms/scratch/ARStream_15-7-2017-22-27-22.txt";
		//string sFilename = "/Users/rms/scratch/ARStream_15-7-2017-23-11-48.txt";

		if (current_filename != sFilename) {
			gs.ARKitStream stream = new gs.ARKitStream();
			stream.InitializeFromFileAscii(sFilename);

			string sVideoFilename = sFilename.Replace("ARStream", "VideoStream");
			sVideoFilename = sVideoFilename.Replace(".txt", ".bin");
			gs.VideoStream video_stream = null;
			if (File.Exists(sVideoFilename)) {
				video_stream = new gs.VideoStream();
				video_stream.InitializeRead(sVideoFilename, true);
			}

			FakeStream = new gs.ARKitStreamPlayback();
			FakeStream.SetSource(stream, video_stream);

			current_filename = sFilename;
		}


		samplesParent = new GameObject("samples_parent");
		GameObject videoPlane = UnityUtil.FindGameObjectByName("VideoPlane");
		videoPlane.SetVisible(true);
		Material videoMaterial = videoPlane.GetMaterial();
		Texture2D video_texture = null;

		FakeStream.NewFrameF = (time) => {
			GameObject.Destroy(samplesParent);
			samplesParent = new GameObject("samples_parent");
		};
		FakeStream.UpdateCameraF = (pos, rot) => {
			Camera.main.transform.position = pos;
			Camera.main.transform.rotation = rot;
		};
		FakeStream.EmitSamplePointF = (pos,screenPos,color) => {
			GameObject go = DebugUtil.EmitDebugSphere("pt", pos, 0.01f, color, null, true);
			samplesParent.AddChild(go, true);

			if ( ParticleGridVizGO != null )
				ParticleGridVizGO.GetComponent<ParticleGridViz>().InjectSample(pos, color);
		};
		FakeStream.EmitVideoFrameF = (frame) => {
			if ( video_texture == null ) {
				video_texture = new Texture2D(frame.width, frame.height);
			}
			video_texture.LoadImage(frame.rgb);
			videoMaterial.SetTexture("_MainTex", video_texture);
		};

		FakeStream.Start(Time.realtimeSinceStartup);
	}


	// Update is called once per frame
	void Update () {

		// clear reset after 2 frames
		if ( reset_counter > 0 ) {
			reset_counter--;
			if (reset_counter == 0)
				gsGlobal.ResetPending = false;
		}

		if (FakeStream != null)
			FakeStream.Update(Time.realtimeSinceStartup);


	}


}
