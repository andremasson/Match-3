/*
 * Copyright (c) 2017 Razeware LLC
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tile : MonoBehaviour {
	private static Color selectedColor = new Color(.5f, .5f, .5f, 1.0f);
	private static Tile previousSelected = null;

	private SpriteRenderer render;
	private bool isSelected = false;

	private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private bool matchFound = false;
    private float animationTime;
    public bool isMoving;

    private Vector2 touchOrigin;

    void Awake() {
		render = GetComponent<SpriteRenderer>();
        animationTime = BoardManager.instance.tileMoveSpeed;
        isMoving = false;
    }

    private void FixedUpdate()
    {
        int horizontalAxis = 0;
        int verticalAxis = 0;

        if (Input.touchCount > 0)
        {
            Touch myTouch = Input.touches[0];
            if (myTouch.phase == TouchPhase.Began)
            {
                Select();
                touchOrigin = myTouch.position;
            }
            else if (myTouch.phase == TouchPhase.Ended)
            {
                Deselect();
                Vector2 touchEnd = myTouch.position;
                horizontalAxis = (int)(touchEnd.x - touchOrigin.x);
                verticalAxis = (int)(touchEnd.y - touchOrigin.y);
            }
        }


        if (horizontalAxis > 0)
        {
            // Move para direita
            SwapTile(Vector2.right);
        }
        else if (horizontalAxis < 0)
        {
            // Move para esquerda
            SwapTile(Vector2.left);
        }
        else if (verticalAxis > 0)
        {
            // Move para cima
            SwapTile(Vector2.up);
        }
        else if (verticalAxis < 0)
        {
            // Move para baixo
            SwapTile(Vector2.down);
        }
    }

    private void Select() {
		isSelected = true;
		render.color = selectedColor;
		previousSelected = gameObject.GetComponent<Tile>();
		SFXManager.instance.PlaySFX(Clip.Select);
	}

	private void Deselect() {
		isSelected = false;
		render.color = Color.white;
		previousSelected = null;
	}

    public void MoveTileTo(Vector3 endPosition, float speed = 0.0f)
    {
        if (speed == 0.0f)
        {
            speed = animationTime;
        }
        Vector3 startPosition = transform.localPosition;
        StartCoroutine(AnimateMove(startPosition, endPosition, speed));
    }

    private IEnumerator AnimateMove(Vector3 startPosition, Vector3 endPosition, float time)
    {
        if (!isMoving)
        {
            isMoving = true;
            float i = 0.0f;
            float rate = 1 / time;
            while (i < 1)
            {
                i += Time.deltaTime * rate;
                transform.localPosition = Vector3.Lerp(startPosition, endPosition, i);
                yield return 0;
            }
            isMoving = false;
            ClearAllMatches();
        }
        yield return 0;
    }
        
    private void OnMouseDown()
    {
        if (render.sprite == null || BoardManager.instance.IsShifting)
        {
            return;
        }

        if (isSelected)
        {
            Deselect();
        }
        else
        {
            if (previousSelected == null) // touch start?
            {
                Select();
            }
            else
            {
                if (GetAllAdjacentTiles().Contains(previousSelected.gameObject))
                {
                    SwapTile(previousSelected.render);
                    previousSelected.Deselect();
                }
                else
                {
                    previousSelected.GetComponent<Tile>().Deselect();
                    Select();
                }
            }
        }
    }

    public void SwapTile(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction);
        if (hit.collider != null)
        {
            SwapTile(hit.collider.gameObject.GetComponent<SpriteRenderer>());
        }
    }
    
    public void SwapTile(SpriteRenderer render2)
    {
        if (render.sprite == render2.sprite)
        {            
            return; 
        }

        render2.GetComponent<Tile>().MoveTileTo(transform.localPosition);
        MoveTileTo(render2.transform.localPosition);

        previousSelected.ClearAllMatches();
        ClearAllMatches();

        GUIManager.instance.MoveCounter--;
        SFXManager.instance.PlaySFX(Clip.Swap);
    }

    private GameObject GetAdjacent(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        if (hit.collider != null)
        {
            return hit.collider.gameObject;
        }
        return null;
    }

    private List<GameObject> GetAllAdjacentTiles()
    {
        List<GameObject> adjacentTiles = new List<GameObject>();
        for (int i = 0; i < adjacentDirections.Length; i++)
        {
            adjacentTiles.Add(GetAdjacent(adjacentDirections[i]));
        }
        return adjacentTiles;
    }

    private List<GameObject> FindMatch(Vector2 castDir)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        while (hit.collider != null && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite)
        {
            matchingTiles.Add(hit.collider.gameObject);
            hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
        }
        return matchingTiles;
    }

    private void ClearMatch(Vector2[] paths)
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        for (int i = 0; i < paths.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(paths[i]));
        }
        if (matchingTiles.Count >= 2)
        {
            for (int i = 0; i < matchingTiles.Count; i++)
            {
                //matchingTiles[i].GetComponent<SpriteRenderer>().sprite = null;
                matchingTiles[i].GetComponent<Tile>().DestroyTile();
                //Plunge();
            }
            
            matchFound = true;
        }
    }

    private void OnDestroy()
    {
    }

    public void DestroyTile()
    {
        BoardManager.instance.SpawnTile(transform.position);
        Destroy(this.gameObject);
    }

    public void ClearAllMatches()
    {
        if (render.sprite == null)
        {
            return;
        }

        ClearMatch(new Vector2[2] { Vector2.left, Vector2.right });
        ClearMatch(new Vector2[2] { Vector2.up, Vector2.down });
        if (matchFound)
        {
            render.sprite = null;
            matchFound = false;
            //StopCoroutine(BoardManager.instance.FindNullTiles());
            //StartCoroutine(BoardManager.instance.FindNullTiles());
            SFXManager.instance.PlaySFX(Clip.Clear);
            DestroyTile();
        }
    }

    private GameObject FindTileAbove()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.up);
        if (hit.collider == null)
        {
            return null;
        }
        return hit.collider.gameObject;
    }
    
    public void Plunge(bool plungeSelf = true)
    {
        Vector3 newPos = transform.localPosition;

        newPos.y -= render.bounds.size.y;
        
        GameObject tileAbove = FindTileAbove();
        if (tileAbove != null)
        {
            tileAbove.GetComponent<Tile>().Plunge();
        }
        
        if (plungeSelf)
        {
            MoveTileTo(newPos, BoardManager.instance.tilePlungeSpeed);
        }
    }
}