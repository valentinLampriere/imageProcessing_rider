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
    private int thresholdValue_blockSize = 201;
    private int dilateIteration = 5;

    private VectorOfVectorOfPoint line;

    private RectangleF area;

    private List<Vector2> terrain;

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
    private VectorOfVectorOfPoint LineDetection(Mat matThresolded, bool draw = true) {
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        Mat hierarchy = new Mat();
        CvInvoke.FindContours(matThresolded, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);

        VectorOfVectorOfPoint approxContours = new VectorOfVectorOfPoint();
        for (int i = 0; i < contours.Size; i++) {

            VectorOfPoint approxContour = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(contours[i], approxContour, CvInvoke.ArcLength(contours[i], true) * 0.01, true);

            approxContours.Push(approxContour);

            if (draw) {
                CvInvoke.Polylines(webcamFrame, approxContour, true,
                        new Bgr(Color.Blue).MCvScalar, 4);
            }

        }
        return approxContours;
    }

    private List<RotatedRect> RectangleDetection(Mat matThresolded, bool draw = true) {
        VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
        Mat hierarchy = new Mat();
        CvInvoke.FindContours(matThresolded, contours, hierarchy, RetrType.List, ChainApproxMethod.ChainApproxNone);

        List<RotatedRect> boxList = new List<RotatedRect>();
        for (int i = 0; i < contours.Size; i++) {
            VectorOfPoint approxContour = new VectorOfPoint();
            CvInvoke.ApproxPolyDP(contours[i], approxContour, CvInvoke.ArcLength(contours[i], true) * 0.05, true);
            double contourArea = CvInvoke.ContourArea(approxContour, false);
            if (contourArea > 250 && contourArea < 10000) {
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
                if (box.GetVertices()[0].X > 5f && box.GetVertices()[3].Y < 450)
                    CvInvoke.Polylines(webcamFrame, System.Array.ConvertAll(box.GetVertices(), Point.Round), true,
                        new Bgr(Color.DeepPink).MCvScalar, 4);
            }
        }
        return boxList;
    }


    private bool SetArea(List<RotatedRect> boxList) {
        if (boxList.Count != 4) return false;

        PointF p1 = boxList[0].GetVertices()[2];
        PointF p2 = boxList[1].GetVertices()[3];
        PointF p3 = boxList[2].GetVertices()[0];
        PointF p4 = boxList[3].GetVertices()[0];

        float epsilon = 0.1f;

        float minX = Mathf.Min(p1.X, p2.X);
        float minY = Mathf.Min(p1.Y, p2.Y);
        float maxX = Mathf.Max(p2.X, p4.X);
        float maxY = Mathf.Max(p3.Y, p4.Y);

        float width = maxX - minX;
        float height = maxY - minY;

        Debug.Log("minX : " + minX + ", maxX : " + maxX );
        Debug.Log("minY : " + minY + ", maxY : " + maxY);
        area = new RectangleF(minX + width * epsilon, minY + height * epsilon, (maxX - minX) - width * epsilon, (maxY - minY) - height * epsilon);

        /*area = new Rectangle2D(
            new Vector2(p1.X, p1.Y),
            new Vector2(p2.X, p2.Y),
            new Vector2(p3.X, p3.Y),
            new Vector2(p4.X, p4.Y)
            );*/


        return true;
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

            //CvInvoke.AdaptiveThreshold(matGrayscale, matThresolded, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, 25, 30);
            //CvInvoke.AdaptiveThreshold(matGrayscale, matThresolded, 255, AdaptiveThresholdType.MeanC, ThresholdType.BinaryInv, thresholdValue_blockSize, thresholdValue_param1);
            //CvInvoke.Threshold(matGrayscale, matThresolded, thresholdValue_param1, 255 ,ThresholdType.Binary);
            CvInvoke.Threshold(matGrayscale, matThresolded, 75, 255 ,ThresholdType.Binary);


            //Mat element = CvInvoke.GetStructuringElement(Emgu.CV.CvEnum.ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1));
            //CvInvoke.Dilate(matThresolded, matThresolded, element, new Point(-1, -1), (int)(dilateIteration / 2), BorderType.Constant, new MCvScalar(255, 255, 255));
            //CvInvoke.Erode(matThresolded, matThresolded, element, new Point(-1, -1), dilateIteration, BorderType.Constant, new MCvScalar(255, 255, 255));

            /*Image<Gray, byte> webcamImg = matThresolded.ToImage<Gray, byte>();
            webcamImg._SmoothGaussian(3);*/

            CvInvoke.CvtColor(matThresolded, webcamFrame, ColorConversion.Gray2Bgr);

            

            List<RotatedRect> boxList = RectangleDetection(matThresolded);

            if (SetArea(boxList)) {


                /*Mat K = new Mat();
                Mat D = new Mat();
                Mat rotation = new Mat();
                Mat translation = new Mat();
                VectorOfPointF imagePoints = new VectorOfPointF();
                imagePoints.Push(new PointF[] { p1, p2, p3, p4});
                Fisheye.Calibrate(imagePoints, imagePoints, new Size(1280, 1024), K, D, rotation, translation, Fisheye.CalibrationFlag.CheckCond, new MCvTermCriteria(30, 0.1)
                );*/

                VectorOfVectorOfPoint copyLine = new VectorOfVectorOfPoint();

                line = LineDetection(matThresolded);
                /*Debug.Log(line.Size);
                for (int i = 0; i < line.Size; i++) {
                    copyLine.Push(new VectorOfPoint());
                    for (int j = 0; i < line[i].Size; j++) {
                        if(line[i][j].X > p1.X && line[i][j].X < p2.X) {
                            if (line[i][j].Y > p3.Y && line[i][j].Y < p4.Y) {
                                copyLine[i].Push(new Point[] { line[i][j] });
                            }
                        }
                    }
                }*/
                /*for (int i = 0; i < line.Size; i++) {
                    Debug.Log(line[i].Size);
                }*/
            }


            
        }
        // making the thread sleep so that things are not happening too fast. might be optional.
        System.Threading.Thread.Sleep(250);
    }


    
    public void ChangeThreshold_blockSize(Slider slider) {
        thresholdValue_blockSize = 3 + (int)(126 * slider.value) * 2;
        Debug.Log("thresholdValue_blockSize : " + thresholdValue_blockSize);
    }
    public void ChangeThreshold_param1(Slider slider) {
        thresholdValue_param1 = 255 * 2 * slider.value - 255;
        Debug.Log("thresholdValue_param1 : " + thresholdValue_param1);
    }
    public void ChangeDilateIteration(Slider slider) {
        dilateIteration = (int)(slider.value * 20);
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

    public void CreateTerrain() {

        Debug.Log("CreateTerrain");

        terrain = new List<Vector2>();
        if (line != null) {
            for (int i = 0; i < line.Size; i++) {
                for (int j = 0; j < line[i].Size; j++) {
                    //if (area.Contains(new Point(line[i][j].X, line[i][j].Y)))
                    if (line[i][j].X < area.Top && line[i][j].X > area.Bottom) {
                        if (line[i][j].Y < area.Right && line[i][j].Y > area.Left)
                            terrain.Add(new Vector2(line[i][j].X, line[i][j].Y));
                    }
                }
            }
        }


        LineRenderer arealr = transform.GetChild(0).GetComponent<LineRenderer>();
        arealr.positionCount = 4;
        arealr.SetPosition(0, new Vector2(area.X, area.Y));
        arealr.SetPosition(1, new Vector2(area.X + area.Width, area.Y));
        arealr.SetPosition(2, new Vector2(area.X + area.Width, area.Y + area.Height));
        arealr.SetPosition(3, new Vector2(area.X, area.Y + area.Height));


        LineRenderer lr = GetComponent<LineRenderer>();

        for (int i = 0; i < terrain.Count; i++) {
            lr.positionCount++;
            lr.SetPosition(lr.positionCount - 1, terrain[i]);
        }
    }

    public List<Vector2> GetTerrain() {
        return terrain;
    }
    public RectangleF GetArea() {
        return area;
    }
}
