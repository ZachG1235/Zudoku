using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TileInput : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int Row { get; private set; }
    public int Col { get; private set; }
    public bool Marked { get; private set; }

    public Sprite crossIcon;
    public Sprite flagIcon;

    public event Action<TileInput> Clicked;
    public event Action<TileInput> DoubleClicked;
    public event Action<TileInput> DragStarted;
    public event Action<TileInput> DragEntered;
    public event Action<TileInput> DragEnded;

    private Image tileIcon;
    private bool isCrossed = false;

    public void Init(int row, int col, bool isMarked)
    {
        Row = row;
        Col = col;
        Marked = isMarked;
    }

    private void Awake()
    {
        tileIcon = transform.GetChild(0).GetComponent<Image>();
        isCrossed = false;
        SetDisplayEmpty();
    }

    private void SetDisplayEmpty()
    {
        tileIcon.sprite = null;
        SetAlpha(tileIcon, 0);
    }

    private void SetDisplayCross()
    {
        tileIcon.sprite = crossIcon;
        SetAlpha(tileIcon, 1);
    }

    public void SetDisplayFlag()
    {
        tileIcon.sprite = flagIcon;
        SetAlpha(tileIcon, 1);
    }

    public void ToggleCross()
    {
        isCrossed = !isCrossed;

        if (isCrossed)
        {
            SetDisplayCross();
        }
        else
        {
            SetDisplayEmpty();
        }
    }
    
    private void SetAlpha(Image image, float alpha)
    {
        Color tempColor = image.color;
        tempColor.a = alpha;
        image.color = tempColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (eventData.clickCount == 2)
        {
            DoubleClicked?.Invoke(this);
        }
        else
        {
            Clicked?.Invoke(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        DragStarted?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData) { }

    public void OnEndDrag(PointerEventData eventData)
    {
        DragEnded?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.dragging && eventData.pointerDrag != null)
        {
            DragEntered?.Invoke(this);
        }
    }
}
