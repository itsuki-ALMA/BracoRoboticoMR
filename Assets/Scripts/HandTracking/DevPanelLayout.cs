using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DevPanelLayout : MonoBehaviour
{
    [Header("Tamanho do painel")]
    [SerializeField]
    private Vector2 canvasSize =
        new Vector2(700f, 400f);

    [Header("Escala no mundo")]
    [SerializeField]
    private float worldScale =
        0.001f;

    [Header("Visual")]
    [SerializeField]
    private Color panelColor =
        new Color(
            0.035f,
            0.045f,
            0.065f,
            0.92f
        );

    [SerializeField]
    private Color textColor =
        new Color(
            0.95f,
            0.97f,
            1f,
            1f
        );

    [SerializeField]
    private Color secondaryTextColor =
        new Color(
            0.75f,
            0.82f,
            0.90f,
            1f
        );

    [SerializeField]
    private Color buttonColor =
        new Color(
            0.10f,
            0.13f,
            0.18f,
            1f
        );

    [SerializeField]
    private Color grabBarColor =
        new Color(
            0.78f,
            0.82f,
            0.88f,
            0.95f
        );

    private RectTransform canvasRect;

    private Transform devPanel;
    private Transform grabBar;

    private TMP_Text title;
    private TMP_Text statusText;

    private Toggle pinToggle;

    // =========================================================
    // TABS
    // =========================================================

    private Transform tabs;

    private Button infoTabButton;
    private Button modelTabButton;

    private Transform infoContent;
    private Transform modelContent;

    // =========================================================
    // INFORMAÇÕES
    // =========================================================

    private Toggle metricsToggle;

    private Transform metricsRoot;

    private TMP_Text handText;
    private TMP_Text ikText;
    private TMP_Text udpText;

    // =========================================================
    // MODELO 3D
    // =========================================================

    private Toggle modelVisibleToggle;

    private TMP_Text modelStateText;

    private Button explodeButton;
    private Button assembleButton;
    private Button resetButton;

    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        Setup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    Setup();
                }
            };
        }
    }
#endif

    // =========================================================
    // SETUP
    // =========================================================

    [ContextMenu("Aplicar Layout DEV")]
    public void Setup()
    {
        FindReferences();

        if (canvasRect == null)
        {
            Debug.LogError(
                "[DEV UI] DevPanelLayout precisa estar em um RectTransform."
            );

            return;
        }

        if (devPanel == null)
        {
            Debug.LogError(
                "[DEV UI] Não encontrei DevPanel."
            );

            return;
        }

        ConfigureCanvas();
        ConfigurePanel();

        ConfigureTitle();
        ConfigureStatus();
        ConfigurePinToggle();

        ConfigureTabs();

        ConfigureInfoContent();
        ConfigureMetricsToggle();
        ConfigureMetricsRoot();
        ConfigureHand();
        ConfigureIK();
        ConfigureUDP();

        ConfigureModelContent();
        ConfigureModelVisibleToggle();
        ConfigureModelState();
        ConfigureModelButtons();

        ConfigureGrabBar();

        Debug.Log(
            "[DEV UI] Layout com abas aplicado."
        );
    }

    // =========================================================
    // REFERÊNCIAS
    // =========================================================

    private void FindReferences()
    {
        canvasRect =
            GetComponent<RectTransform>();

        devPanel =
            transform.Find(
                "DevPanel"
            );

        if (devPanel == null)
        {
            return;
        }

        title =
            FindTMP(
                devPanel,
                "Title"
            );

        statusText =
            FindTMP(
                devPanel,
                "StatusText"
            );

        Transform pin =
            devPanel.Find(
                "PinToggle"
            );

        if (pin != null)
        {
            pinToggle =
                pin.GetComponent<Toggle>();
        }

        tabs =
            devPanel.Find(
                "Tabs"
            );

        if (tabs != null)
        {
            Transform infoTab =
                tabs.Find(
                    "InfoTabButton"
                );

            if (infoTab != null)
            {
                infoTabButton =
                    infoTab.GetComponent<Button>();
            }

            Transform modelTab =
                tabs.Find(
                    "Model3DTabButton"
                );

            if (modelTab != null)
            {
                modelTabButton =
                    modelTab.GetComponent<Button>();
            }
        }

        infoContent =
            devPanel.Find(
                "InfoContent"
            );

        modelContent =
            devPanel.Find(
                "Model3DContent"
            );

        // Compatibilidade com a hierarquia antiga.
        Transform metricsToggleTransform =
            infoContent != null
                ? infoContent.Find("MetricsToggle")
                : null;

        if (metricsToggleTransform == null)
        {
            metricsToggleTransform =
                devPanel.Find(
                    "MetricsToggle"
                );
        }

        if (metricsToggleTransform != null)
        {
            metricsToggle =
                metricsToggleTransform
                    .GetComponent<Toggle>();
        }

        metricsRoot =
            infoContent != null
                ? infoContent.Find("MetricsRoot")
                : null;

        if (metricsRoot == null)
        {
            metricsRoot =
                devPanel.Find(
                    "MetricsRoot"
                );
        }

        if (metricsRoot != null)
        {
            handText =
                FindTMP(
                    metricsRoot,
                    "HandText"
                );

            ikText =
                FindTMP(
                    metricsRoot,
                    "IKText"
                );

            udpText =
                FindTMP(
                    metricsRoot,
                    "UDPText"
                );
        }

        if (modelContent != null)
        {
            Transform modelToggle =
                modelContent.Find(
                    "ModelVisibleToggle"
                );

            if (modelToggle != null)
            {
                modelVisibleToggle =
                    modelToggle.GetComponent<Toggle>();
            }

            modelStateText =
                FindTMP(
                    modelContent,
                    "ModelStateText",
                    false
                );

            Transform explode =
                modelContent.Find(
                    "ExplodeButton"
                );

            if (explode != null)
            {
                explodeButton =
                    explode.GetComponent<Button>();
            }

            Transform assemble =
                modelContent.Find(
                    "AssembleButton"
                );

            if (assemble != null)
            {
                assembleButton =
                    assemble.GetComponent<Button>();
            }

            Transform reset =
                modelContent.Find(
                    "ResetButton"
                );

            if (reset != null)
            {
                resetButton =
                    reset.GetComponent<Button>();
            }
        }

        grabBar =
            devPanel.Find(
                "GrabBar"
            );

        if (grabBar == null)
        {
            // Compatibilidade com a versão antiga.
            grabBar =
                transform.Find(
                    "GrabBar"
                );
        }
    }

    // =========================================================
    // CANVAS / PAINEL
    // =========================================================

    private void ConfigureCanvas()
    {
        canvasRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        canvasRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        canvasRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        canvasRect.sizeDelta =
            canvasSize;

        canvasRect.localScale =
            Vector3.one *
            worldScale;

        canvasRect.localRotation =
            Quaternion.identity;
    }

    private void ConfigurePanel()
    {
        RectTransform rect =
            devPanel.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        rect.localPosition =
            new Vector3(
                rect.localPosition.x,
                rect.localPosition.y,
                0f
            );

        Image image =
            devPanel.GetComponent<Image>();

        if (image != null)
        {
            image.color =
                panelColor;

            image.raycastTarget =
                false;
        }
    }

    // =========================================================
    // CABEÇALHO
    // =========================================================

    private void ConfigureTitle()
    {
        if (title == null)
        {
            return;
        }

        ConfigureText(
            title,
            new Vector2(
                20f,
                -18f
            ),
            new Vector2(
                260f,
                45f
            ),
            new Vector2(
                0f,
                1f
            ),
            30f,
            textColor
        );

        title.text =
            "DEV PANEL";

        title.fontStyle =
            FontStyles.Bold;
    }

    private void ConfigureStatus()
    {
        if (statusText == null)
        {
            return;
        }

        ConfigureText(
            statusText,
            new Vector2(
                -20f,
                -20f
            ),
            new Vector2(
                300f,
                40f
            ),
            new Vector2(
                1f,
                1f
            ),
            22f,
            textColor
        );

        statusText.alignment =
            TextAlignmentOptions.TopRight;
    }

    private void ConfigurePinToggle()
    {
        if (pinToggle == null)
        {
            return;
        }

        RectTransform rect =
            pinToggle.GetComponent<RectTransform>();

        if (rect == null)
        {
            return;
        }

        SetRect(
            rect,
            new Vector2(
                -20f,
                -62f
            ),
            new Vector2(
                200f,
                35f
            ),
            new Vector2(
                1f,
                1f
            )
        );

        TMP_Text label =
            pinToggle.GetComponentInChildren<
                TMP_Text
            >(true);

        if (label != null)
        {
            label.text =
                "Fixar painel";

            label.fontSize =
                20f;

            label.color =
                secondaryTextColor;

            label.enableWordWrapping =
                false;
        }
    }

    // =========================================================
    // TABS
    // =========================================================

    private void ConfigureTabs()
    {
        if (tabs == null)
        {
            return;
        }

        RectTransform rect =
            tabs.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.anchorMin =
                new Vector2(
                    0f,
                    1f
                );

            rect.anchorMax =
                new Vector2(
                    1f,
                    1f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );

            rect.anchoredPosition =
                new Vector2(
                    0f,
                    -100f
                );

            rect.sizeDelta =
                new Vector2(
                    -30f,
                    46f
                );

            rect.localScale =
                Vector3.one;

            rect.localRotation =
                Quaternion.identity;
        }

        ConfigureTabButton(
            infoTabButton
        );

        ConfigureTabButton(
            modelTabButton
        );
    }

    private void ConfigureTabButton(
        Button button
    )
    {
        if (button == null)
        {
            return;
        }

        Image image =
            button.GetComponent<Image>();

        if (image != null)
        {
            if (image.color.a <= 0.01f)
            {
                image.color =
                    buttonColor;
            }

            image.raycastTarget =
                true;
        }

        TMP_Text label =
            button.GetComponentInChildren<
                TMP_Text
            >(true);

        if (label != null)
        {
            label.fontSize =
                19f;

            label.color =
                textColor;

            label.alignment =
                TextAlignmentOptions.Center;

            label.raycastTarget =
                false;
        }
    }

    // =========================================================
    // ABA INFORMAÇÕES
    // =========================================================

    private void ConfigureInfoContent()
    {
        if (infoContent == null)
        {
            return;
        }

        ConfigureContentRoot(
            infoContent
        );
    }

    private void ConfigureMetricsToggle()
    {
        if (metricsToggle == null)
        {
            return;
        }

        RectTransform rect =
            metricsToggle.GetComponent<
                RectTransform
            >();

        if (rect == null)
        {
            return;
        }

        SetRect(
            rect,
            new Vector2(
                15f,
                -10f
            ),
            new Vector2(
                235f,
                36f
            ),
            new Vector2(
                0f,
                1f
            )
        );

        TMP_Text label =
            metricsToggle.GetComponentInChildren<
                TMP_Text
            >(true);

        if (label != null)
        {
            label.text =
                "Mostrar métricas";

            label.fontSize =
                20f;

            label.color =
                secondaryTextColor;

            label.enableWordWrapping =
                false;
        }
    }

    private void ConfigureMetricsRoot()
    {
        if (metricsRoot == null)
        {
            return;
        }

        RectTransform rect =
            metricsRoot.GetComponent<
                RectTransform
            >();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            new Vector2(
                15f,
                15f
            );

        rect.offsetMax =
            new Vector2(
                -15f,
                -55f
            );

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        rect.localPosition =
            new Vector3(
                rect.localPosition.x,
                rect.localPosition.y,
                0f
            );
    }

    private void ConfigureHand()
    {
        if (handText == null)
        {
            return;
        }

        ConfigureText(
            handText,
            new Vector2(
                10f,
                -10f
            ),
            new Vector2(
                180f,
                180f
            ),
            new Vector2(
                0f,
                1f
            ),
            20f,
            textColor
        );

        handText.text =
            "HAND\n" +
            "X: 0%\n" +
            "Y: 0%\n" +
            "Z: 0%\n" +
            "Garra: 0%";
    }

    private void ConfigureIK()
    {
        if (ikText == null)
        {
            return;
        }

        ConfigureText(
            ikText,
            new Vector2(
                205f,
                -10f
            ),
            new Vector2(
                280f,
                190f
            ),
            new Vector2(
                0f,
                1f
            ),
            18f,
            textColor
        );

        ikText.text =
            "IK\n" +
            "Target X: 0\n" +
            "Target Y: 0\n" +
            "Target Z: 0\n" +
            "Base: 0°\n" +
            "Frente/Tras: 0°\n" +
            "Vertical: 0°\n" +
            "Válida: NÃO";
    }

    private void ConfigureUDP()
    {
        if (udpText == null)
        {
            return;
        }

        ConfigureText(
            udpText,
            new Vector2(
                -10f,
                -10f
            ),
            new Vector2(
                180f,
                180f
            ),
            new Vector2(
                1f,
                1f
            ),
            18f,
            textColor
        );

        udpText.text =
            "UDP\nNenhum pacote enviado";
    }

    // =========================================================
    // ABA MODELO 3D
    // =========================================================

    private void ConfigureModelContent()
    {
        if (modelContent == null)
        {
            return;
        }

        ConfigureContentRoot(
            modelContent
        );
    }

    private void ConfigureModelVisibleToggle()
    {
        if (modelVisibleToggle == null)
        {
            return;
        }

        RectTransform rect =
            modelVisibleToggle.GetComponent<
                RectTransform
            >();

        if (rect != null)
        {
            rect.anchorMin =
                new Vector2(
                    0f,
                    1f
                );

            rect.anchorMax =
                new Vector2(
                    1f,
                    1f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );

            rect.anchoredPosition =
                new Vector2(
                    0f,
                    -12f
                );

            rect.sizeDelta =
                new Vector2(
                    0f,
                    42f
                );

            rect.localScale =
                Vector3.one;

            rect.localRotation =
                Quaternion.identity;
        }

        TMP_Text label =
            modelVisibleToggle
                .GetComponentInChildren<
                    TMP_Text
                >(true);

        if (label != null)
        {
            label.text =
                "Mostrar modelo 3D";

            label.fontSize =
                21f;

            label.color =
                textColor;

            label.enableWordWrapping =
                false;
        }
    }

    private void ConfigureModelState()
    {
        if (modelStateText == null)
        {
            return;
        }

        RectTransform rect =
            modelStateText.rectTransform;

        rect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        rect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        rect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        rect.anchoredPosition =
            new Vector2(
                0f,
                -64f
            );

        rect.sizeDelta =
            new Vector2(
                0f,
                34f
            );

        modelStateText.fontSize =
            22f;

        modelStateText.color =
            secondaryTextColor;

        modelStateText.alignment =
            TextAlignmentOptions.Center;

        modelStateText.raycastTarget =
            false;
    }

    private void ConfigureModelButtons()
    {
        ConfigureActionButton(
            explodeButton,
            0f,
            0.49f,
            -108f
        );

        ConfigureActionButton(
            assembleButton,
            0.51f,
            1f,
            -108f
        );

        ConfigureActionButton(
            resetButton,
            0f,
            1f,
            -166f
        );
    }

    private void ConfigureActionButton(
        Button button,
        float anchorMinX,
        float anchorMaxX,
        float y
    )
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect =
            button.GetComponent<
                RectTransform
            >();

        if (rect != null)
        {
            rect.anchorMin =
                new Vector2(
                    anchorMinX,
                    1f
                );

            rect.anchorMax =
                new Vector2(
                    anchorMaxX,
                    1f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    1f
                );

            rect.anchoredPosition =
                new Vector2(
                    0f,
                    y
                );

            rect.sizeDelta =
                new Vector2(
                    0f,
                    46f
                );

            rect.localScale =
                Vector3.one;

            rect.localRotation =
                Quaternion.identity;
        }

        Image image =
            button.GetComponent<Image>();

        if (image != null)
        {
            image.color =
                buttonColor;

            image.raycastTarget =
                true;
        }

        TMP_Text label =
            button.GetComponentInChildren<
                TMP_Text
            >(true);

        if (label != null)
        {
            label.fontSize =
                20f;

            label.color =
                textColor;

            label.alignment =
                TextAlignmentOptions.Center;

            label.raycastTarget =
                false;
        }
    }

    // =========================================================
    // CONTENT ROOT
    // =========================================================

    private void ConfigureContentRoot(
        Transform content
    )
    {
        RectTransform rect =
            content.GetComponent<
                RectTransform
            >();

        if (rect == null)
        {
            return;
        }

        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            new Vector2(
                20f,
                28f
            );

        rect.offsetMax =
            new Vector2(
                -20f,
                -155f
            );

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        rect.localPosition =
            new Vector3(
                rect.localPosition.x,
                rect.localPosition.y,
                0f
            );
    }

    // =========================================================
    // GRAB BAR
    // =========================================================

    private void ConfigureGrabBar()
    {
        if (grabBar == null)
        {
            return;
        }

        RectTransform rect =
            grabBar.GetComponent<
                RectTransform
            >();

        if (rect != null)
        {
            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0f
                );

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchoredPosition =
                new Vector2(
                    0f,
                    -20f
                );

            rect.sizeDelta =
                new Vector2(
                    100f,
                    7f
                );

            rect.localScale =
                Vector3.one;

            rect.localRotation =
                Quaternion.identity;
        }

        Image image =
            grabBar.GetComponent<Image>();

        if (image != null)
        {
            image.color =
                grabBarColor;

            // O collider real fica no GrabAnchor independente.
            image.raycastTarget =
                false;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private void ConfigureText(
        TMP_Text text,
        Vector2 position,
        Vector2 size,
        Vector2 anchor,
        float fontSize,
        Color color
    )
    {
        RectTransform rect =
            text.rectTransform;

        SetRect(
            rect,
            position,
            size,
            anchor
        );

        text.fontSize =
            fontSize;

        text.color =
            color;

        text.alignment =
            TextAlignmentOptions.TopLeft;

        text.enableWordWrapping =
            false;

        text.overflowMode =
            TextOverflowModes.Overflow;

        text.raycastTarget =
            false;
    }

    private void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size,
        Vector2 anchor
    )
    {
        rect.anchorMin =
            anchor;

        rect.anchorMax =
            anchor;

        rect.pivot =
            anchor;

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;

        rect.localPosition =
            new Vector3(
                rect.localPosition.x,
                rect.localPosition.y,
                0f
            );
    }

    private TMP_Text FindTMP(
        Transform parent,
        string objectName,
        bool warn = true
    )
    {
        if (parent == null)
        {
            return null;
        }

        Transform child =
            parent.Find(
                objectName
            );

        if (child == null)
        {
            if (warn)
            {
                Debug.LogWarning(
                    "[DEV UI] Não encontrei " +
                    objectName
                );
            }

            return null;
        }

        return child.GetComponent<TMP_Text>();
    }
}
