using UnityEngine;

public class ArenaBallAttachmentPoints : MonoBehaviour
{
    [SerializeField] private Transform heldBallAnchor;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private GameObject heldBallVisual;

    public Transform HeldBallAnchor => heldBallAnchor;
    public Transform ThrowOrigin => throwOrigin;
    public GameObject HeldBallVisual => heldBallVisual;

    public void Configure(Transform newHeldBallAnchor, Transform newThrowOrigin, GameObject newHeldBallVisual)
    {
        heldBallAnchor = newHeldBallAnchor;
        throwOrigin = newThrowOrigin;
        heldBallVisual = newHeldBallVisual;
    }
}
