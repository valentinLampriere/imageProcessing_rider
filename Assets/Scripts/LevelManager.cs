using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private Material[] cubesMat; // 0 blue 1 red
    [SerializeField] private Manager manager;

    private List<Vector2> points;

    void Awake()
    {
        points = new List<Vector2>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //List<Vector2> points = new List<Vector2>();
        //float x = -10f;
        //float y = 0f;

        //for (int i = 0; i < 100; i++)
        //{
        //    x += Random.Range(1.0f, 3.0f);
        //    y += Random.Range(-2.0f, 2.0f);
        //    points.Add(new Vector2(x, y));
        //}

        ////points.Add(new Vector2(2, 2));
        ////points.Add(new Vector2(10, 5));

        //CreateLine(points);

        //CreateRectangleCube(new Vector2(0, 3), new Vector2(2, 0), new Vector2(5, 5), new Vector2(7, 2), Color.red);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateLine(List<Vector2> points)
    {
        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateLineCube(points[i], points[i + 1], cubePrefab);
        }
    }

    public void CreateRectangleCube(Vector2 _p1, Vector2 _p2, Vector2 _p3, Vector2 _p4, Color color)
    {
        Vector2 pl = (_p1 + _p2) / 2f;
        Vector2 pr = (_p3 + _p4) / 2f;

        GameObject rectangle = CreateLineCube(pl, pr, obstaclePrefab);
        float scaleY = Vector2.Distance(_p1, _p2);
        Vector3 currentScale = rectangle.transform.localScale;
        rectangle.transform.localScale = new Vector3(currentScale.x, scaleY, currentScale.z);

        ChangeRectangleColor(rectangle, color);
    }

    public void ChangeRectangleColor(GameObject rectangle, Color color)
    {
        MeshRenderer mesh = rectangle.GetComponent<MeshRenderer>();

        if (color == Color.blue)
        {
            mesh.material = cubesMat[0];
        }
        else if (color == Color.red)
        {
            mesh.material = cubesMat[1];
        }

        Color c = mesh.material.color;
        rectangle.GetComponent<Obstacle>().CurrentColor = c;
        mesh.material.color = new Color(c.r, c.g, c.b, 0.3f);
    }

    public GameObject CreateLineCube(Vector2 _p1, Vector2 _p2, GameObject objectType)
    {
        Vector2 pos = (_p1 + _p2) / 2f;
        //pos += new Vector2(0.5f, -0.5f);
        float scale = Vector2.Distance(_p1, _p2);
        float rotate = Mathf.Rad2Deg * Mathf.Atan2(_p2.y - _p1.y, _p2.x - _p1.x);

        GameObject cube = Instantiate(objectType, pos, Quaternion.Euler(0, 0, rotate));
        cube.transform.localScale = new Vector3(scale + (Mathf.Abs(rotate) * 0.001f), 0.06f, 1);

        return cube;
    }

    public void SetTerrain(List<Vector2> _points)
    {
        points = _points;

        CreateLine(points);
    }
}
