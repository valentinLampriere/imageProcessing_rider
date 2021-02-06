using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using Color = System.Drawing.Color;

public class Manager : MonoBehaviour {

    // Webcam
    private Emgu.CV.VideoCapture webcam;
    private Mat webcamFrame;

    [SerializeField]
    private RawImage rawImage;
    private Texture2D tex;

    private float thresholdValue_param1 = 1;
    private int thresholdValue_blockSize = 25;

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

    private List<RotatedRect> RectangleDetection(Mat matThresolded, bool draw = true) {
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        Mat hierarchy = new Mat();
        CvInvoke.FindContours(matThresolded, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);

        List<RotatedRect> boxList = new List<RotatedRect>();
        for (int i = 0; i < contours.Size; i++) {
            VectorOfPoint approxContour = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(contours[i], approxContour, CvInvoke.ArcLength(contours[i], true) * 0.05, true);

            if (CvInvoke.ContourArea(approxContour, false) > 250) {
                if (approxContour.Size == 4) {
                    #region determine if all the angles in the contour are within [80, 100] degree
                    bool isRectangle = true;
                    Point[] pts = approxContour.ToArray();
                    LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                    for (int j = 0; j < edges.Length; j++) {
                        double angle = Mathf.Abs((float)edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                        if (angle < 80 || angle > 100) {
                            isRectangle = false;
                            break;
                        }
                    }

                    #endregion

                    if (isRectangle) boxList.Add(CvInvoke.MinAreaRect(approxContour));
                }
            }
        }
        if (draw) {
            foreach (RotatedRect box in boxList) {
                CvInvoke.Polylines(webcamFrame, System.Array.ConvertAll(box.GetVertices(), Point.Round), true,
                    new Bgr(Color.DeepPink).MCvScalar, 4);
            }
        }
        return boxList;
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

            Debug.Log(thresholdValue_blockSize);

            //CvInvoke.AdaptiveThreshold(matGrayscale, matThresolded, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, thresholdValue_blockSize, thresholdValue_param1);
            //CvInvoke.Threshold(matGrayscale, matThresolded, thresholdValue_param1, 255 ,ThresholdType.Binary);
            CvInvoke.Threshold(matGrayscale, matThresolded, 100, 255 ,ThresholdType.Binary);


            CvInvoke.CvtColor(matThresolded, webcamFrame, ColorConversion.Gray2Bgr);

            List<RotatedRect> boxList = RectangleDetection(matThresolded);

            if (boxList.Count == 4) {
                
            }
        }
        // making the thread sleep so that things are not happening too fast. might be optional.
        System.Threading.Thread.Sleep(200);
    }

    
    public void ChangeThreshold_blockSize(Slider slider) {
        thresholdValue_blockSize = 3 + (int)(126 * slider.value) * 2;
    }
    public void ChangeThreshold_param1(Slider slider) {
        thresholdValue_param1 = 255 * 2 * slider.value - 255;
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
