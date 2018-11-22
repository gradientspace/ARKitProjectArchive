# ARKit Recorder/Simulator utility for Unity #

Experiment with ARKit data without iOS 11.
Copyright (C) Ryan Schmidt / Gradientspace 2017.
MIT License.

Questions / Suggestions? [@rms80](https://twitter.com/rms80) on twitter or [gradientspace.com](www.gradientspace.com)

## WHAT IS THIS FOR?

ARKit Recorder does two things. On your device, you can use it to record a stream of ARKit data, including the tracked camera position, 3D point cloud, and horizontal plane anchors. It can also store video frames (however this is very slow).

In the Unity Editor, this code can load and play back recorded sessions, and generates events you can hook into. In addition, because playback does not depend on the ARKit API at all, it works without iOS 11. You can play back This means you can play back ARKit streams on **Windows** or **OSX** desktop, or in an app where you aren't actually running ARKit. 

This is mainly intended for Development, although it is also quite useful to run repeatable demos. Unity provides a live ARKit connection via Unity Remote  (See [video](https://www.youtube.com/watch?v=kxPOCYb_1Y8)). However, this naturally runs at a much lower framerate than on-device, which affects ARKit tracking, and it also takes some time to get running. A canned recording of an ARKit session can make debugging much easier (in my experience).

There is a short [demo video here](https://twitter.com/rms80/status/886428111516168195). This demo includes the in-Editor video playback, as you can see it is quite choppy. The playback of the ARKit data without the video is muich smoother.


## PLAYBACK TEST

To test this code, check out the [ARKitToolsDemoProject](https://bitbucket.org/gradientspace/arkittoolsdemoproject) repo, which includes ARKitRecorder as a submodule, as well as a fork of the Unity ARKit plugin with a few small changes (to access pixel colors and the video stream). Then follow these instructions:

1. unzip **Recordings/ARStream_16-7-2017-22-25-2.txt.zip** (to same folder)
2. open **RecorderDemo1** scene
3. hit Play in Editor
4. click Play button in bottom-right
5. After a few seconds you should see many brown spheres appearing, as in the image below. These are the ARKit point cloud points each frame, colored by pixel color. Eventually a blue plane will appear. If you hit Pause you can inspect the geometry in the Scene view.

![file sharing](https://bytebucket.org/gradientspace/arkitrecorder/raw/c66e529803a37bbff852fb3e531355306124be5a/doc/playback_in_editor.png)

## Recording

The ARKitToolsDemoProject can also be built as an iOS app for recording your own ARKit streams. To do this, you have to follow these steps:

1. Set the **Platform** to **iOS** in **Build Settings**
2. in **Player Settings**, add your iOS Developer Team ID
3. Build and Deploy to the device

You should end up with an app on your phone/ipad called **ARKitToolsDemoProject**. You can rename it by changing the **Product Name** and **Bundle Identifier** in the **Build Seettings**.

When you run this app, the **Play** button that you see in the Editor will instead say **Record**. When you click that, you will get a popup with a **Start** button and an option to record video. Don't check that option for now. During recording, the **Record** button will change to a **Stop** button.

#### Getting Recorded Streams Off your Device

Once you have made a recording, you need to download it from your device. You can do this using **iTunes File Sharing**. Connect your device to your Mac, then open iTunes and go the main panel for the Device. Scroll down in the right panel past the list of Apps and you will find a **File Sharing** section, that looks like the image below. 

Click on the recorder app and you will see a list of stream files (prefixed with *ARStream*) on the right that you can drag-and-drop onto your local disk. 

![iTunes file sharing](https://bytebucket.org/gradientspace/arkitrecorder/raw/c66e529803a37bbff852fb3e531355306124be5a/doc/iTunesSharing.png)


#### Video Streams

The recorder can also try to capture a video frame for each ARKit "frame", ie video frames where the ARKit data changed. Currently this is very slow, and signficantly affects ARKit tracking, so it is probably not ready to actually use yet. When you try, though, a file *VideoStream*_< date >.bin will be saved for the ARStream with the corresponding name. 




## API Usage


#### Recording

Recording is done through the **ARKitStreamRecorder** class, which is a Singleton object, not a MonoBehavior. You don't need to attach it to a GameObject. Simply call the **StartRecording()** and **StopRecording()** methods. The **IsRecording** field will tell you if recording is active.

To capture video, set the **EnableSaveVideoStream** field to true. You can also experiment with the **TargetVideoFrameRate** field, although the default of 10fps is already faster than the current code can capture.

Internally, ARKitStreamRecorder is creating an instance of **ARKitStream** and then passing the stream the relevant info (start-frame, camera position, point cloud, plane anchor updates). During recording this data is immediately written to the stream file, so if you get an assert/crash, the data might still be recorded (which is helpful for debugging). If you are capturing video a **VideoStream** will also be created. 


#### Playback

In the sample project, the **ARKitSimulator** script is attached to an empty GameObject in the scene (also called *ARKitSimulator*). This script has a few options to configure the playback behavior, however the **StartPlayback(filename)** function must be called by some other code. The **Test()** function does this with a hardcoded path to the included sample stream, this is what the current UI in the scene does.

Internally, **ARKitSimulator** opens the stream file and rebuilds the **ARKitStream** object, and then initializes an instance of **ARKitStreamPlayback** (named *FakeStream*), which is a class that parses the stream and emits the simulated ARKit events as time progresses.

By default, **ARKitSimulator** listens for these events and does several things. It updates the camera position, it adds small colored spheres to the scene for each active point-cloud point, and it adds and updates the identified planes. It also shows the video frames, if available and a suitable camera-alined is provided.

However, **ARKitSimulator** is mainly meant as a demo of how to use **ARKitStreamPlayback**. If you are going to use this component, you probably will use that class directly. Once you have opened the stream, there are a series of callback functions you can set, such as *NewFrameF*, *UpdateCameraF*, *EmitSamplePointF*, etc. As long as you call the **Update()** method of **ARKitStreamPlayback** from some MonoBehavior.Update() method attached to a GameObject, these callbacks will be called at (apprxoimately) the right wall-clock time.

**ARKitSimulator.StartPlayback()** configures these callbacks for the stream it loads and plays, so that is a good place to look to see how to implement standard behavior. Or you can copy and edit ARKitSimulator to avoid having to figure all that stuff out.




## Sample Project Notes

To enable iTunes File Sharing, you have to add a line to **Info.plist** for the app. Unity auto-generates this file during the Build process. To automatically add this line, there is a script **Editor\XCodePlistEdits.cs** in the demo project. You can just drop this script into any other app to enable iTunes File Sharing.

The 2D UI Canvas in the sample app (named *ARKitrecorderUI*) is controlled by an attached script claled **ARKitRecorderUI**. 



## TODO

* working video playback (is it broken?)
* Binary stream (faster writes & reads)
* reduced resolution video stream / faster capture method / faster playback
* use built-in iOS video recording? (if possible?)

