using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System.Drawing;
using System;
using Emgu.CV.Util;
using UnityEngine.UI;
using MyBox;

public class ColorDetection : MonoBehaviour
{
    private Mat input;
    private int l;

    private List<RotatedRect> RedRects;
    private List<RotatedRect> BlueRects;

    public Manager manager;
    public LevelManager lm;

    private void Start()
    {
        l = 0;
        input = CvInvoke.Imread($"{Application.dataPath}/sample5.png", ImreadModes.AnyColor);
        RedRects = new List<RotatedRect>();
        BlueRects = new List<RotatedRect>();

        ShowRectangles();
        lm.CreateRectangles();
    }

    private void ShowRectangles()
    {
        Image<Bgr, byte> currentImage = input.ToImage<Bgr, byte>();
        Image<Bgr, byte> copyImage = input.ToImage<Bgr, byte>();
        Image<Bgr, byte> copyImage2 = input.ToImage<Bgr, byte>();
        
        currentImage._SmoothGaussian(5);

        DetectRed(copyImage, l);
        DetectBlue(copyImage2, l);

        RedRects = FindRectangles(copyImage.Mat, "Red");
        BlueRects = FindRectangles(copyImage2.Mat, "Blue");

        Mat RedMat = GetMatRectangles(RedRects, copyImage.Size);
        Mat BlueMat = GetMatRectangles(BlueRects, copyImage2.Size);

        CvInvoke.Imshow("RedRects", RedMat.ToImage<Bgr, byte>());
        CvInvoke.Imshow("BlueRects", BlueMat.ToImage<Bgr, byte>());

        l++;
    }

    private List<RotatedRect> FindRectangles(Mat img, string debugString = null)
    {
        UMat cannyEdges = new UMat();
        UMat gray = new UMat();

        double cannyThreshold = 250.0;
        double cannyThresholdLinking = 120.0;

        CvInvoke.CvtColor(img, gray, ColorConversion.Bgr2Gray);
        CvInvoke.GaussianBlur(gray, gray, new Size(3, 3), 1);

        CvInvoke.Canny(gray, cannyEdges, cannyThreshold, cannyThresholdLinking);

        List<RotatedRect> boxList = new List<RotatedRect>();
        using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
        {
            CvInvoke.FindContours(cannyEdges, contours, null, RetrType.List,
                ChainApproxMethod.ChainApproxSimple);
            int count = contours.Size;
            for (int i = 0; i < count; i++)
            {
                using (VectorOfPoint contour = contours[i])
                using (VectorOfPoint approxContour = new VectorOfPoint())
                {
                    CvInvoke.ApproxPolyDP(contour, approxContour, CvInvoke.ArcLength(contour, true) * 0.05, true);
                    
                    if (approxContour.Size == 4) //The contour has 4 vertices.
                    {
                        bool isRectangle = true;
                        Point[] pts = approxContour.ToArray();
                        LineSegment2D[] edges = PointCollection.PolyLine(pts, true);

                        for (int j = 0; j < edges.Length; j++)
                        {
                            double angle = Math.Abs(
                                edges[(j + 1) % edges.Length].GetExteriorAngleDegree(edges[j]));
                            if (angle < 80 || angle > 100)
                            {
                                isRectangle = false;
                                break;
                            }
                        }

                        if (isRectangle)
                        {
                            /*
                            var abc = CvInvoke.MinAreaRect(approxContour);
                            Debug.Log(abc);
                            Debug.Log(abc.Center);
                            Debug.Log(abc.GetVertices().Length);
                            */
                            boxList.Add(CvInvoke.MinAreaRect(approxContour));
                        }
                    }
                }
            }
        }

        if (debugString != null)
        {
            Debug.Log($"{debugString} : {boxList.Count}");
        }

        return boxList;
    }

    private Mat GetMatRectangles(List<RotatedRect> boxList, Size imgSize)
    {
        Mat rectangleImage = new Mat(imgSize, DepthType.Cv8U, 3);

        rectangleImage.SetTo(new MCvScalar(0));
        int boxcount = 0;
        foreach (RotatedRect box in boxList)
        {
            CvInvoke.Polylines(rectangleImage, Array.ConvertAll(box.GetVertices(), Point.Round), true,
                new Bgr(System.Drawing.Color.DarkOrange).MCvScalar);
        }

        return rectangleImage;
    }

    private void CheckNesting(List<RotatedRect> boxList)
    {
        foreach(RotatedRect rect in boxList)
        {
            PointF point0 = rect.GetVertices()[0];
            PointF point1 = rect.GetVertices()[1];
            PointF point2 = rect.GetVertices()[2];
            PointF point3 = rect.GetVertices()[3];
        }
    }

    private float CalculateTriangleArea(float Ax, float Ay, float Bx, float By, float Cx, float Cy)
    {
        float area;

        area = Math.Abs((Bx * Ay - Ax * By) + (Cx * By - Bx * Cy) + (Ax * Cy - Cx * Ay)) / 2;

        return area;
    }

    private Image<Gray, byte> DetectRed(Image<Bgr, byte> image, int l)
    {
        Bgr lower = new Bgr(0, 0, 100);
        Bgr higher = new Bgr(50, 50, 255);

        Image <Gray, byte> mask = image.InRange(lower, higher).Not();
        image.SetValue(new Bgr(0, 0, 0), mask);

        if (l % 10 == 0)
            CvInvoke.Imshow($"Red {l}", image);

        return mask;
    }

    private Image<Gray, byte> DetectGreen(Image<Bgr, byte> image, int l)
    {
        Bgr lower = new Bgr(0, 0, 0);
        Bgr higher = new Bgr(50, 255, 50);

        Image<Gray, byte> mask = image.InRange(lower, higher).Not();
        image.SetValue(new Bgr(0, 0, 0), mask);

        if (l % 10 == 0)
            CvInvoke.Imshow($"Green {l}", image);

        return mask;
    }

    private Image<Gray, byte> DetectBlue(Image<Bgr, byte> image, int l)
    {
        Bgr lower = new Bgr(100, 0, 0);
        Bgr higher = new Bgr(255, 100, 100);

        Image<Gray, byte> mask = image.InRange(lower, higher).Not();
        image.SetValue(new Bgr(0, 0, 0), mask);

        if (l % 10 == 0)
            CvInvoke.Imshow($"Blue {l}", image);

        return mask;
    }

    public List<List<PointF>> GetRedRectangles()
    {
        List<List<PointF>> rects = new List<List<PointF>>();

        foreach (RotatedRect rect in RedRects)
        {
            List<PointF> rectVerts = new List<PointF>();
            
            foreach(PointF point in rect.GetVertices())
            {
                rectVerts.Add(point);
            }

            rects.Add(rectVerts);
        }

        return rects;
    }

    public List<List<PointF>> GetBlueRectangles()
    {
        List<List<PointF>> rects = new List<List<PointF>>();

        foreach (RotatedRect rect in BlueRects)
        {
            List<PointF> rectVerts = new List<PointF>();

            foreach (PointF point in rect.GetVertices())
            {
                rectVerts.Add(point);
            }

            rects.Add(rectVerts);
        }

        return rects;
    }

    //Unity Editor debug functions
    [ButtonMethod()]
    private void ReopenWindows()
    {
        FuckTheWindows();
        ShowRectangles();
    }

    [ButtonMethod()]
    private void FuckTheWindows()
    {
        CvInvoke.DestroyAllWindows();
    }
}
