using UnityEngine;
using Yarn.Unity;

public class WheelMover : MonoBehaviour
{
    [SerializeField] Transform wheel;

    [YarnCommand("move_wheel")]
    public void MoveWheel(int distance)
    {
        var pos = wheel.position;
        pos.y += distance;
        wheel.position = pos;
    }
}
