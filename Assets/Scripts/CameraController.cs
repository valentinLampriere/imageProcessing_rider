using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform playerTransform;
    [SerializeField][Range(0.01f, 1f)] private float stiffness;

    private Vector3 offset;

    void Awake()
    {
        offset = transform.position - playerTransform.position;
    }

    void LateUpdate()
    {
        transform.position = Vector3.Slerp(transform.position, playerTransform.position + offset, stiffness);
    }
}
