using UnityEngine;

[System.Serializable]
public class WallData
{
    public Vector3 position;
    public Vector3 scale;
    public bool isHorizontal;
    public int gridX;
    public int gridY;
    public GameObject wallObject;
}