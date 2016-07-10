using UnityEngine;
using System.Collections;

public class CursorScript : MonoBehaviour {
	public Texture2D cursorTexture;
	public CursorMode cursorMode = CursorMode.ForceSoftware;
	void OnMouseEnter() {
		cursorTexture.width = cursorTexture.width / 4;
			Cursor.SetCursor(cursorTexture, new Vector2(cursorTexture.width/4, cursorTexture.height/4), cursorMode);
		Cursor.visible = true;
	}
	void OnMouseExit() {
		Cursor.SetCursor(null, Vector2.zero, cursorMode);
	}
}