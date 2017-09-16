using UnityEngine;
using System.Collections;

public class HexCell : MonoBehaviour {
	public TextMesh textObj;
	public GameObject hexObj;
	MeshRenderer hexRenderer; 

	public float elevation;

	private Mesh m_mesh;
	private Color32[] colArray;
	private int m_colorPropertyId;

//	public void Init() {
//	}

	// Use this for initialization
	void Awake () {
		//m_mesh = hexObj.GetComponent<MeshFilter>().mesh;
		hexRenderer = hexObj.GetComponent<MeshRenderer>();
		//colArray = new Color32[m_mesh.vertexCount];
		m_colorPropertyId = Shader.PropertyToID("_Color");
	}
//	
	// Update is called once per frame
//	void Update () {
//	
//	}

	public void SetText(string text){
		textObj.text = text;
	}

	public string getText()
	{
		return textObj.text;
	}

	public void ShowText(){
		textObj.gameObject.SetActive(true);
	}

	public void HideText(){
		textObj.gameObject.SetActive(false);
	}

//	public void SetMaterial(Material material){
//		hexRenderer.material = material;
//	}

	public void SetColor (Color color)
	{
		MaterialPropertyBlock block = new MaterialPropertyBlock();
		hexRenderer.GetPropertyBlock(block);
		block.SetColor(m_colorPropertyId, color);
		hexRenderer.SetPropertyBlock(block);
	}

//	public void SetColor (Color color){
//		Color32 col32 = color;
//		colArray = new Color32[m_mesh.vertexCount];
//		for (int i = 0; i < colArray.Length; i++)
//		{
//			colArray[i] = col32;
//		}
//		m_mesh.colors32 = colArray;
//		//hexRenderer.material.color = color;
//	}
	
}
