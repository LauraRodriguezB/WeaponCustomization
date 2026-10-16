using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(Image))]
public class TabButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Sprite ActiveSprite;
    public Sprite HoverSprite;
    public Sprite NormalSprite;
    private Image _buttonImage;

    private bool isSelected = false;

    void Awake()
    {
        _buttonImage = GetComponent<Image>();
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisualState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isSelected && HoverSprite != null && _buttonImage != null)
        {
            _buttonImage.sprite = HoverSprite;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        if (_buttonImage == null)
        {
            return;
        }

        if (isSelected && ActiveSprite != null)
        {
            _buttonImage.sprite = ActiveSprite;
        }
        else if (!isSelected && NormalSprite != null)
        {
            _buttonImage.sprite = NormalSprite;
        }
    }
}