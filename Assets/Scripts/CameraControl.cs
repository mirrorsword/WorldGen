using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;

public class CameraControl : MonoBehaviour {
	public float zoomSpeed = 1f;

	float zoom;
	Camera cam;
	Plane plane;
	Vector3 oldMousePostion;
	Vector3 oldPosition;
	bool panActive;
	//bool isDragging = false;

	// Use this for initialization
	void Start () {
		cam = GetComponent<Camera>();
		zoom = Mathf.Sqrt(cam.orthographicSize);
		plane = new Plane(Vector3.back, 0);
	}
	
	// Update is called once per frame
	void Update () {
		float mouseScroll = Input.GetAxis("Mouse ScrollWheel");
		if (mouseScroll!= 0)
		{
			zoom += mouseScroll * -1 * zoomSpeed; 
			cam.orthographicSize = Mathf.Pow(zoom,2);
		}

		if (Input.GetMouseButtonDown(0) && ! EventSystem.current.IsPointerOverGameObject())
		{
			oldMousePostion = MouseWorldPostion();
			panActive = true;
		}

		else if (Input.GetMouseButton(0) && panActive)
		{
			Vector3 newMousePostion = MouseWorldPostion();
			Vector3 offset = newMousePostion - oldMousePostion;
			transform.position -= offset;
			oldMousePostion = MouseWorldPostion();
		}
		else if (Input.GetMouseButtonUp(0))
		{
			panActive = false;
		}

		Vector3 pos = transform.position;
		pos.y += Input.GetAxis("Vertical")/3;
		pos.x += Input.GetAxis("Horizontal")/3;
		transform.position = pos;
	
	}

	public Vector3 MouseWorldPostion()
	{
		Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		float dist;
		plane.Raycast(ray, out dist);
		Vector3 worldPostion = ray.GetPoint(dist);
		return worldPostion;
	}



	public void SetOrthoSize(float size)
	{
		if (cam == null)
			return;
		cam.orthographicSize = size;
		zoom = Mathf.Sqrt(cam.orthographicSize);
	}
}
