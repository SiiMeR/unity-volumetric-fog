using UnityEngine;

public class MoveSun : MonoBehaviour
{

	public bool shouldMove;
	public float moveSpeed;

	// Update is called once per frame
	void Update () {
		if (shouldMove)
		{
			transform.Rotate(Vector3.right, moveSpeed * Time.unscaledDeltaTime);
		}
	}
}
