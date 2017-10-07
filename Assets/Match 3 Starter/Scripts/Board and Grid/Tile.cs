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

public class Tile : MonoBehaviour
{
	public static Color selectedColor = new Color(.5f, .5f, .5f, 1.0f);
    public static Color deleteHighlightColor = Color.yellow;
    //private static GameObject previousSelected = null;

    public Transform target;

    public SpriteRenderer render;
	public bool isSelected = false;
    public int points = 30;

	private Vector2[] adjacentDirections = new Vector2[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    private bool matchFound = false;
    private float animationTime;

    float lastUpdate = 0.0f;
    float updateSpeed = .1f;

    public bool isMoving;
    public struct IntVector2
    {
        public int x, y;
        public IntVector2(int newX, int newY)
        {
            x = newX;
            y = newY;
        }
    }

    public IntVector2 positionInArray;
    private Vector2 touchOrigin;

    public Vector3 fixedPosition;
    
    void Awake() {
		render = GetComponent<SpriteRenderer>();
        isMoving = false;
    }

    private void Start()
    {
        animationTime = BoardManager.instance.tileMoveSpeed;
    }

    private void FixedUpdate()
    {
        if (lastUpdate > updateSpeed)
        {
            lastUpdate = 0.0f;
            //Plunge();
        }
        else
        {
            lastUpdate += Time.deltaTime;
        }
    }

    /*
    public void Select() {
		//isSelected = true;
		render.color = selectedColor;
		//previousSelected = gameObject;
		SFXManager.instance.PlaySFX(Clip.Select);
	}

	public void Deselect() {
		//isSelected = false;
		render.color = Color.white;
		//previousSelected = null;
	}
    */

        /*
    public void MoveTo(Vector3 endPosition, float speed = 0.0f)
    {
        if (speed == 0.0f)
        {
            speed = animationTime;
        }
        Vector3 startPosition = transform.localPosition;        
        StartCoroutine(AnimateMove(startPosition, endPosition, speed));
        BoardManager.instance.tiles[PositionInArray.x, PositionInArray.y] = gameObject;
    }

    private IEnumerator AnimateMove(Vector3 startPosition, Vector3 endPosition, float time)
    {
        if (!isMoving)
        {
            isMoving = true;
            BoardManager.instance.movingCount++;
            float i = 0.0f;
            float rate = 1 / time;
            while (i < 1)
            {
                i += Time.deltaTime * rate;
                transform.localPosition = Vector3.Lerp(startPosition, endPosition, i);
                yield return 0;
            }
            isMoving = false;
            if (BoardManager.instance.movingCount > 0) BoardManager.instance.movingCount--;
            
            StartCoroutine(ClearAllMatches());
            Plunge();
        }
        yield return 0;
    }
    */
    
    /*
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
                    SwapTile(previousSelected);
                    previousSelected.GetComponent<Tile>().Deselect();
                }
                else
                {
                    previousSelected.GetComponent<Tile>().Deselect();
                    Select();
                }
            }
        }
    }
    */

    /*
    public void SwapTile(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction);
        if (hit.collider != null)
        {
            SwapTile(hit.collider.gameObject);
        }
    }
    */
    
    /*
    public void SwapTile(GameObject anotherObject)
    {
        Tile anotherTile = anotherObject.GetComponent<Tile>();
        if (render.sprite == anotherTile.GetComponent<SpriteRenderer>().sprite)
        {            
            return; 
        }

        if (!GetAllAdjacentTiles().Contains(anotherObject))
        {
            return;
        }

        IntVector2 tempPos = PositionInArray;
        Vector3 tempFixedPosition = fixedPosition;
        PositionInArray = anotherTile.PositionInArray;
        fixedPosition = anotherTile.fixedPosition;
        anotherTile.PositionInArray = tempPos;
        anotherTile.fixedPosition = tempFixedPosition;
        
        anotherTile.MoveTo(fixedPosition, BoardManager.instance.tileMoveSpeed);
        MoveTo(anotherTile.fixedPosition, BoardManager.instance.tileMoveSpeed);
        
        GUIManager.instance.MoveCounter--;
        SFXManager.instance.PlaySFX(Clip.Swap);
    }
    */
   
    private float GetAdjacentDistance(Vector2 castDir)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, castDir);
        if (hit.collider != null)
        {
            return hit.distance;
        }
        return float.NaN;
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
        while (hit.collider != null && hit.collider.gameObject.tag == "Tile" && hit.collider.GetComponent<SpriteRenderer>().sprite == render.sprite)
        {
            matchingTiles.Add(hit.collider.gameObject);
            hit = Physics2D.Raycast(hit.collider.transform.position, castDir);
        }
        return matchingTiles;
    }

    public List<GameObject> FindAllMatches()
    {
        List<GameObject> matchingTiles = new List<GameObject>();
        Vector2[] directions = new Vector2[4] { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
        for (int i = 0; i < directions.Length; i++)
        {
            matchingTiles.AddRange(FindMatch(directions[i]));
        }
        return matchingTiles;
    }

    /*
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
            }
            
            matchFound = true;
        }
    }
    */
    
    /*
    public void DestroyTile()
    {
        if (isMoving)
        {
            BoardManager.instance.movingCount--;
        }
        render.sprite = null;
        BoardManager.instance.tiles[PositionInArray.x, PositionInArray.y] = null;
        //BoardManager.instance.PlungeTiles();
        Destroy(this.gameObject);
    }
    */
    
    /*
    public IEnumerator ClearAllMatches()
    {
        
        yield return new WaitUntil(() => BoardManager.instance.movingCount == 0);

        ClearMatch(new Vector2[2] { Vector2.left, Vector2.right });
        ClearMatch(new Vector2[2] { Vector2.up, Vector2.down });
        if (matchFound)
        {
            render.sprite = null;
            matchFound = false;
            //StopCoroutine(BoardManager.instance.ShiftTilesDown());
            //StartCoroutine(BoardManager.instance.ShiftTilesDown());
            
            SFXManager.instance.PlaySFX(Clip.Clear);
            DestroyTile();
        }
    }
    
    public void Plunge() 
    {
        float dist = GetAdjacentDistance(Vector2.down);
        if (dist >= render.bounds.size.y)
        {
            Vector3 endPosition = transform.localPosition;
            endPosition.y -= render.bounds.size.y;

            //GameObject adjacentTile = GetAdjacent(Vector2.down);
            fixedPosition = endPosition;

            //PositionInArray.y += 1;
            //BoardManager.instance.tiles[PositionInArray.x, PositionInArray.y] = gameObject;
            MoveTo(endPosition, BoardManager.instance.tilePlungeSpeed);
        }
    }
    */
}