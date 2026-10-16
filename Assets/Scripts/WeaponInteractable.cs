using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class WeaponInteractable : MonoBehaviour
{
    public Camera WeaponCamera;

    public GameObject BarrelPart;
    public GameObject MagazinePart;
    public GameObject OpticsPart;
    public GameObject OptionButtonPrefab;
    public GameObject StockPart;
    public GameObject UnderBarrelPart;

    public Material HighlightMaterial;

    public RawImage WeaponDisplayImage;

    public TabButton BarrelTab;
    public TabButton MagazineTab;
    public TabButton OpticsTab;
    public TabButton StockTab;
    public TabButton UnderBarrelTab;

    public TMP_Text SelectText;
    public TMP_Text TitleText;

    public Transform CamBarrel;
    public Transform CamDefault;
    public Transform CamMagazine;
    public Transform CamOptics;
    public Transform CamStock;
    public Transform CamUnderBarrel;
    public Transform ScrollContentParent;

    public WeaponPartCategory[] PartCategories;

    public float ZoomSpeed = 5f;
    public string DefaultSelectText = "Select a part to customize";
    public string DefaultTitle = "";

    [System.Serializable]
    public struct WeaponVariantOption
    {
        public GameObject variantObject;
        public Sprite variantIcon;
    }

    [System.Serializable]
    public struct WeaponPartCategory
    {
        public string categoryKey;
        public WeaponVariantOption[] allVariantsOfThisPart;
    }

    private Dictionary<string, GameObject> _currentlyEquippedVariants = new Dictionary<string, GameObject>();
    private GameObject _currentHighlightedPart;
    private Material _originalMaterial;
    private Renderer _partRenderer;
    private TabButton _activeTab;
    private Transform _currentTargetCam;
    private bool _isHoveringUI = false;
    private bool _isZoomed = false;

    void Start()
    {
        if (WeaponCamera != null && CamDefault != null)
        {
            _currentTargetCam = CamDefault;
        }

        if (TitleText != null)
        {
            TitleText.text = DefaultTitle;
        }

        if (SelectText != null)
        {
            SelectText.text = DefaultSelectText;
            SelectText.gameObject.SetActive(true);
        }
    }

    void Update()
    {
        if (Mouse.current == null || WeaponCamera == null || WeaponDisplayImage == null)
        {
            return;
        }

        WeaponCamera.transform.position = Vector3.Lerp(WeaponCamera.transform.position, _currentTargetCam.position, Time.deltaTime * ZoomSpeed);
        WeaponCamera.transform.rotation = Quaternion.Slerp(WeaponCamera.transform.rotation, _currentTargetCam.rotation, Time.deltaTime * ZoomSpeed);

        if (_isHoveringUI)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        RectTransform rectTransform = WeaponDisplayImage.rectTransform;
        GameObject hitPart = null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, null, out Vector2 localMousePos))
        {
            float width = rectTransform.rect.width;
            float height = rectTransform.rect.height;

            float normalizedX = (localMousePos.x + rectTransform.pivot.x * width) / width;
            float normalizedY = (localMousePos.y + rectTransform.pivot.y * height) / height;

            if (normalizedX >= 0f && normalizedX <= 1f && normalizedY >= 0f && normalizedY <= 1f)
            {
                Ray ray = WeaponCamera.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0));

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitPart = hit.collider.gameObject;

                    if (Mouse.current.leftButton.wasPressedThisFrame)
                    {
                        SelectPart(hitPart);
                    }
                }
                else
                {
                    if (Mouse.current.leftButton.wasPressedThisFrame && _isZoomed)
                    {
                        ResetCameraView();
                    }
                }
            }
        }

        if (hitPart != null)
        {
            if (_currentHighlightedPart != hitPart)
            {
                ResetCurrentHighlight();
                _currentHighlightedPart = hitPart;
                _partRenderer = _currentHighlightedPart.GetComponent<Renderer>();

                if (_partRenderer != null && HighlightMaterial != null)
                {
                    _originalMaterial = _partRenderer.sharedMaterial;
                    _partRenderer.material = HighlightMaterial;
                }
            }
        }
        else
        {
            ResetCurrentHighlight();
        }
    }

    void ResetCurrentHighlight()
    {
        if (_currentHighlightedPart != null)
        {
            Renderer rend = _currentHighlightedPart.GetComponent<Renderer>();
            if (rend != null && _originalMaterial != null)
            {
                rend.material = _originalMaterial;
            }
            _currentHighlightedPart = null;
        }
    }

    public void SelectPart(GameObject part)
    {
        string partName = part.name.ToLower();
        string partType = "";
        string displayName = "";

        if (SelectText != null)
        {
            SelectText.gameObject.SetActive(false);
        }

        if (partName.Contains("underbarrel"))
        {
            _currentTargetCam = CamUnderBarrel != null ? CamUnderBarrel : CamDefault;
            partType = "underbarrel";
            displayName = "UNDER BARREL";
        }
        else if (partName.Contains("magazine"))
        {
            _currentTargetCam = CamMagazine != null ? CamMagazine : CamDefault;
            partType = "magazine";
            displayName = "MAGAZINE";
        }
        else if (partName.Contains("barrel"))
        {
            _currentTargetCam = CamBarrel != null ? CamBarrel : CamDefault;
            partType = "barrel";
            displayName = "BARREL";
        }
        else if (partName.Contains("stock"))
        {
            _currentTargetCam = CamStock != null ? CamStock : CamDefault;
            partType = "stock";
            displayName = "STOCK";
        }
        else if (partName.Contains("optics"))
        {
            _currentTargetCam = CamOptics != null ? CamOptics : CamDefault;
            partType = "optics";
            displayName = "OPTICS";
        }

        _isZoomed = true;
        HighlightSelectedTab(partType);

        if (TitleText != null)
        {
            TitleText.text = displayName;
        }

        PopulateOptionsPanel(partType);
    }

    void PopulateOptionsPanel(string categoryKey)
    {
        if (ScrollContentParent == null || OptionButtonPrefab == null) return;

        foreach (Transform child in ScrollContentParent)
        {
            Destroy(child.gameObject);
        }

        WeaponPartCategory targetCategory = default;
        bool found = false;
        foreach (var cat in PartCategories)
        {
            if (cat.categoryKey.ToLower() == categoryKey.ToLower())
            {
                targetCategory = cat;
                found = true;
                break;
            }
        }

        if (!found) return;

        if (!_currentlyEquippedVariants.ContainsKey(categoryKey))
        {
            foreach (var variant in targetCategory.allVariantsOfThisPart)
            {
                if (variant.variantObject != null && variant.variantObject.activeSelf)
                {
                    _currentlyEquippedVariants[categoryKey] = variant.variantObject;
                    break;
                }
            }
        }

        GameObject currentlyEquippedObj = _currentlyEquippedVariants.ContainsKey(categoryKey) ? _currentlyEquippedVariants[categoryKey] : null;

        for (int i = 0; i < targetCategory.allVariantsOfThisPart.Length; i++)
        {
            WeaponVariantOption variantData = targetCategory.allVariantsOfThisPart[i];
            GameObject variantObj = variantData.variantObject;
            if (variantObj == null) continue;

            GameObject newButton = Instantiate(OptionButtonPrefab, ScrollContentParent);

            Image[] btnImages = newButton.GetComponentsInChildren<Image>();
            if (btnImages.Length > 0 && variantData.variantIcon != null)
            {
                for (int imgIdx = 0; imgIdx < btnImages.Length; imgIdx++)
                {
                    if (btnImages[imgIdx].gameObject != newButton)
                    {
                        btnImages[imgIdx].sprite = variantData.variantIcon;
                        btnImages[imgIdx].preserveAspect = true;
                        break;
                    }
                }
            }

            Image mainButtonImage = newButton.GetComponent<Image>();
            bool isEquipped = (variantObj == currentlyEquippedObj);

            Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.4f);
            Color hoverColor = new Color(1f, 0.85f, 0.1f, 1f);
            Color equippedBgColor = new Color(0.1f, 1f, 0f, 1f);

            if (mainButtonImage != null)
            {
                mainButtonImage.color = isEquipped ? equippedBgColor : normalColor;
            }

            Transform equippedIndicator = newButton.transform.Find("Equipped");
            if (equippedIndicator != null)
            {
                equippedIndicator.gameObject.SetActive(isEquipped);
            }

            UnityEngine.EventSystems.EventTrigger trigger = newButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            if (trigger == null)
            {
                trigger = newButton.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            }

            GameObject groupToCheck = variantObj;
            GameObject[] fullGroup = new GameObject[targetCategory.allVariantsOfThisPart.Length];
            for (int j = 0; j < targetCategory.allVariantsOfThisPart.Length; j++)
            {
                fullGroup[j] = targetCategory.allVariantsOfThisPart[j].variantObject;
            }

            string catKey = categoryKey;

            var enterEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            enterEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter;
            enterEntry.callback.AddListener((data) => {
                PreviewVariant(fullGroup, groupToCheck);
                if (mainButtonImage != null)
                {
                    mainButtonImage.color = hoverColor;
                }
            });
            trigger.triggers.Add(enterEntry);

            var exitEntry = new UnityEngine.EventSystems.EventTrigger.Entry();
            exitEntry.eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit;
            exitEntry.callback.AddListener((data) => {
                if (_currentlyEquippedVariants.ContainsKey(catKey))
                {
                    PreviewVariant(fullGroup, _currentlyEquippedVariants[catKey]);
                }

                if (mainButtonImage != null)
                {
                    bool equippedCheck = (_currentlyEquippedVariants.ContainsKey(catKey) && _currentlyEquippedVariants[catKey] == groupToCheck);
                    mainButtonImage.color = equippedCheck ? equippedBgColor : normalColor;
                }
            });
            trigger.triggers.Add(exitEntry);

            Button btnComponent = newButton.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.onClick.AddListener(() => {
                    EquipVariant(catKey, fullGroup, groupToCheck);
                    PopulateOptionsPanel(catKey);
                });
            }
        }
    }

    void PreviewVariant(GameObject[] variantGroup, GameObject variantToPreview)
    {
        foreach (GameObject v in variantGroup)
        {
            if (v != null)
            {
                bool isActive = (v == variantToPreview);
                v.SetActive(isActive);

                if (isActive)
                {
                    ResetCurrentHighlight();
                    _currentHighlightedPart = v;
                    _partRenderer = v.GetComponent<Renderer>();
                    if (_partRenderer != null && HighlightMaterial != null)
                    {
                        _originalMaterial = _partRenderer.sharedMaterial;
                        _partRenderer.material = HighlightMaterial;
                    }
                }
            }
        }
    }

    void EquipVariant(string categoryKey, GameObject[] variantGroup, GameObject variantToEquip)
    {
        _currentlyEquippedVariants[categoryKey] = variantToEquip;
        PreviewVariant(variantGroup, variantToEquip);
        HighlightSelectedTab(categoryKey);
    }

    public void OnButtonHover(string partType)
    {
        _isHoveringUI = true;

        GameObject target = null;
        string cleanKey = partType.ToLower();

        if (_currentlyEquippedVariants.ContainsKey(cleanKey))
        {
            target = _currentlyEquippedVariants[cleanKey];
        }
        else
        {
            target = GetPartByType(cleanKey);
        }

        if (target != null && _currentHighlightedPart != target)
        {
            ResetCurrentHighlight();
            _currentHighlightedPart = target;
            _partRenderer = _currentHighlightedPart.GetComponent<Renderer>();
            if (_partRenderer != null && HighlightMaterial != null)
            {
                _originalMaterial = _partRenderer.sharedMaterial;
                _partRenderer.material = HighlightMaterial;
            }
        }
    }

    public void OnButtonExit()
    {
        _isHoveringUI = false;
        ResetCurrentHighlight();
    }

    public void OnButtonClickedUI(string partType)
    {
        GameObject target = GetPartByType(partType);
        if (target != null)
        {
            SelectPart(target);
        }
    }

    private GameObject GetPartByType(string type)
    {
        switch (type.ToLower())
        {
            case "magazine": return MagazinePart;
            case "barrel": return BarrelPart;
            case "stock": return StockPart;
            case "optics": return OpticsPart;
            case "underbarrel": return UnderBarrelPart;
            default: return null;
        }
    }

    private void HighlightSelectedTab(string partType)
    {
        TabButton targetTab = null;
        switch (partType.ToLower())
        {
            case "magazine": targetTab = MagazineTab; break;
            case "barrel": targetTab = BarrelTab; break;
            case "stock": targetTab = StockTab; break;
            case "optics": targetTab = OpticsTab; break;
            case "underbarrel": targetTab = UnderBarrelTab; break;
        }

        if (_activeTab != null && _activeTab != targetTab)
        {
            _activeTab.SetSelected(false);
        }

        if (targetTab != null)
        {
            _activeTab = targetTab;
            _activeTab.SetSelected(true);
        }
    }

    public void ResetCameraView()
    {
        _currentTargetCam = CamDefault;
        _isZoomed = false;
        ResetCurrentHighlight();

        if (_activeTab != null)
        {
            _activeTab.SetSelected(false);
            _activeTab = null;
        }

        if (TitleText != null)
        {
            TitleText.text = DefaultTitle;
        }

        if (SelectText != null)
        {
            SelectText.text = DefaultSelectText;
            SelectText.gameObject.SetActive(true);
        }

        if (ScrollContentParent != null)
        {
            foreach (Transform child in ScrollContentParent)
            {
                Destroy(child.gameObject);
            }
        }

        Debug.Log("Back to default view");
    }
}