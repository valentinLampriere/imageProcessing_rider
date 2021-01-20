using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

public class Manager : MonoBehaviour {

    // Webcam
    private Emgu.CV.VideoCapture webcam;
    private Mat webcamFrame;

    [SerializeField]
    private RawImage rawImage;
    private Texture2D tex;

    private float thresholdValue = 127;

    void Start() {
        if (rawImage == null)
            return;

        webcam = new Emgu.CV.VideoCapture(0, VideoCapture.API.DShow);
        webcamFrame = new Mat();


        webcam.ImageGrabbed += new System.EventHandler(HandleWebcamQueryFrame);
        // Demarage de la webcam
        webcam.Start();
    }

    void Update() {
        if (webcam.IsOpened) {
            bool grabbed = webcam.Grab();

            if (!grabbed) {
                Debug.Log("no more grab");
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
                return;
            }
            DisplayFrameOnPlane();
        }
    }

    private void HandleWebcamQueryFrame(object sender, System.EventArgs e) {
        if (webcam.IsOpened) {
            webcam.Retrieve(webcamFrame);
        }

        // we access data, to not cause double access, use locks !
        lock (webcamFrame) {
            Mat matGrayscale = new Mat(webcamFrame.Width, webcamFrame.Height, DepthType.Cv8U, 1);
            Mat matThresolded = new Mat(webcamFrame.Width, webcamFrame.Height, DepthType.Cv8U, 1);
            CvInvoke.CvtColor(webcamFrame, matGrayscale, ColorConversion.Bgr2Gray);

            CvInvoke.Threshold(matGrayscale, matThresolded, thresholdValue, 255 ,ThresholdType.Binary);


            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            CvInvoke.FindContours(matThresolded, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);
            CvInvoke.CvtColor(matThresolded, webcamFrame, ColorConversion.Gray2Bgr);

            for (int i = 0; i < contours.Size; i++) {
                CvInvoke.DrawContours(webcamFrame, contours, i, new MCvScalar(255, 0, 255), 2);
            }

        }
        // making the thread sleep so that things are not happening too fast. might be optional.
        System.Threading.Thread.Sleep(200);
    }

    public void ChangeThreshold(Slider slider) {
        thresholdValue = 255 * slider.value;
    }

    private void DisplayFrameOnPlane() {
        if (webcamFrame.IsEmpty) {
            return;
        }

        int width = (int)rawImage.rectTransform.rect.width;
        int height = (int)rawImage.rectTransform.rect.height;

        // destroy existing texture
        if (tex != null) {
            Destroy(tex);
            tex = null;
        }

        // creating new texture to hold our frame
        tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

        // Resize mat to the texture format
        CvInvoke.Resize(webcamFrame, webcamFrame, new System.Drawing.Size(width, height));
        // Convert to unity texture format ( RGBA )
        CvInvoke.CvtColor(webcamFrame, webcamFrame, ColorConversion.Bgr2Rgba);
        // Flipping because unity texture is inverted.
        CvInvoke.Flip(webcamFrame, webcamFrame, FlipType.Vertical);
        CvInvoke.Flip(webcamFrame, webcamFrame, FlipType.Horizontal);

        // loading texture in texture object
        tex.LoadRawTextureData(webcamFrame.ToImage<Rgba, byte>().Bytes);
        tex.Apply();

        // assigning texture to gameObject
        rawImage.texture = tex;
    }

    void OnDestroy() {
        if (webcam != null) {
            //waiting for thread to finish before disposing the camera...(took a while to figure out)
            System.Threading.Thread.Sleep(50);
            // close camera
            webcam.Stop();
            webcam.Dispose();
        }
    }
}
