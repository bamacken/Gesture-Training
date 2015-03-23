using UnityEngine;

public class ProjectileDragging : MonoBehaviour {
	public float maxStretch = 3.0f;
	public LineRenderer catapultLineFront;
	public LineRenderer catapultLineBack;  
	public SpringJoint2D spring;
	public bool clickedOn;
	
	private Ray leftCatapultToProjectile;
	private float circleRadius;
	private Vector2 prevVelocity;

	void OnEnable()
	{
		leftCatapultToProjectile = new Ray(catapultLineFront.transform.position, Vector3.zero);
		CircleCollider2D circle = GetComponent<Collider2D>() as CircleCollider2D;
		circleRadius = circle.radius;

		LineRendererSetup ();
		UpdateDrag (false);
	}

	void Awake () 
	{
		spring = GetComponent <SpringJoint2D> ();
	}

	public void UpdateDrag (bool isPinch) 
	{
		if (spring != null) 
		{
			if (!GetComponent<Rigidbody2D>().isKinematic && prevVelocity.sqrMagnitude > GetComponent<Rigidbody2D>().velocity.sqrMagnitude) 
			{
				Destroy (spring);
				GetComponent<Rigidbody2D>().velocity = prevVelocity;
			}

			if (!isPinch)
				prevVelocity = GetComponent<Rigidbody2D>().velocity;
			
			LineRendererUpdate ();
			
		}
		else 
		{
			catapultLineFront.enabled = false;
			catapultLineBack.enabled = false;
		}
	}
	void LineRendererSetup () 
	{
		catapultLineFront.SetPosition(0, catapultLineFront.transform.position);
		catapultLineBack.SetPosition(0, catapultLineBack.transform.position);
		
		catapultLineFront.sortingLayerName = "Foreground";
		catapultLineBack.sortingLayerName = "Foreground";
		
		catapultLineFront.sortingOrder = 3;
		catapultLineBack.sortingOrder = 1;
	}

	public void PinchDragging (Vector3 position) 
	{
		transform.Translate(new Vector3(-position.x,position.y, position.z));
	}

	void LineRendererUpdate () 
	{
		Vector2 catapultToProjectile = transform.position - catapultLineFront.transform.position;
		leftCatapultToProjectile.direction = catapultToProjectile;
		Vector3 holdPoint = leftCatapultToProjectile.GetPoint(catapultToProjectile.magnitude + circleRadius);
		catapultLineFront.SetPosition(1, holdPoint);
		catapultLineBack.SetPosition(1, holdPoint);
	}
}
