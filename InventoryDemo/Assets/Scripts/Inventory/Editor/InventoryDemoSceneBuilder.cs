using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Arma de forma automatica una escena de prueba para entender como funciona
// el sistema de inventario + barra de accion (repo Deme94/InventorySystem).
// Uso: en el Editor de Unity, menu "Tools > Inventory Demo > Build Scene".
public static class InventoryDemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/InventoryDemo.unity";
    private const string ItemsFolder = "Assets/Items";

    [MenuItem("Tools/Inventory Demo/Build Scene")]
    public static void BuildScene()
    {
        if (!AssetDatabase.IsValidFolder(ItemsFolder))
            AssetDatabase.CreateFolder("Assets", "Items");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Items de ejemplo ---
        var emptyHands = CreateDemoActionItem("EmptyHands", "Manos vacias", stackable: false);
        var potion = CreateDemoActionItem("Potion", "Pocion", stackable: true);
        var wood = CreateResourceItem("Wood", "Madera");

        // --- Camara (sin esto, el Game View no limpia el frame anterior y el
        // drag & drop deja "rastro" pintado en vez de moverse limpio) ---
        var cameraGo = new GameObject("Main Camera", typeof(Camera));
        cameraGo.tag = "MainCamera";
        var cam = cameraGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cam.transform.position = new Vector3(0, 0, -10);

        // --- EventSystem ---
        var eventSystemGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // --- Canvas ---
        var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        // --- Player (logica, sin sprite: es una demo de UI/inventario) ---
        var playerGo = new GameObject("Player", typeof(PlayerMovement), typeof(PlayerActions), typeof(Inventory));
        playerGo.tag = "Player";
        var inputManagerGo = new GameObject("InputManager", typeof(InputManager));

        // --- Panel Inventario (arriba, grilla de 20 = tamano fijo del array en Inventory.cs) ---
        var inventoryPanel = CreateUIPanel(canvasGo.transform, "Panel_Inventory",
            anchorMin: new Vector2(0.5f, 1f), anchorMax: new Vector2(0.5f, 1f), pivot: new Vector2(0.5f, 1f),
            anchoredPos: new Vector2(0, -20));
        var invGrid = inventoryPanel.AddComponent<GridLayoutGroup>();
        invGrid.cellSize = new Vector2(48, 48);
        invGrid.spacing = new Vector2(4, 4);
        invGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        invGrid.constraintCount = 10;
        var invFitter = inventoryPanel.AddComponent<ContentSizeFitter>();
        invFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        invFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        inventoryPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);

        var invIcons = new Image[20];
        var invQuantities = new Text[20];
        var invSlotGos = new GameObject[20];
        for (int i = 0; i < 20; i++)
        {
            var slot = CreateSlot(inventoryPanel.transform, $"InvSlot_{i}", out var icon, out var qty);
            invIcons[i] = icon;
            invQuantities[i] = qty;
            invSlotGos[i] = slot;
        }

        // --- Panel ActionBar (abajo, 10 = tamano fijo del array en ActionBar.cs) ---
        var actionBarPanel = CreateUIPanel(canvasGo.transform, "Panel_ActionBar",
            anchorMin: new Vector2(0.5f, 0f), anchorMax: new Vector2(0.5f, 0f), pivot: new Vector2(0.5f, 0f),
            anchoredPos: new Vector2(0, 20));
        var barLayout = actionBarPanel.AddComponent<HorizontalLayoutGroup>();
        barLayout.spacing = 4;
        barLayout.childControlWidth = false;
        barLayout.childControlHeight = false;
        var barFitter = actionBarPanel.AddComponent<ContentSizeFitter>();
        barFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        barFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        actionBarPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.35f);

        var barIcons = new Image[10];
        var barSlotGos = new GameObject[10];
        for (int i = 0; i < 10; i++)
        {
            var slot = CreateSlot(actionBarPanel.transform, $"BarSlot_{i}", out var icon, out _, withQuantity: false);
            barIcons[i] = icon;
            barSlotGos[i] = slot;
        }

        // --- Icono arrastrado (drag & drop) ---
        var draggedGo = new GameObject("DraggedIcon", typeof(Image), typeof(DragAndDrop));
        draggedGo.transform.SetParent(canvasGo.transform, false);
        var draggedImage = draggedGo.GetComponent<Image>();
        draggedImage.raycastTarget = false;
        draggedImage.enabled = false;
        var draggedRect = draggedGo.GetComponent<RectTransform>();
        draggedRect.sizeDelta = new Vector2(40, 40);
        var dragAndDrop = draggedGo.GetComponent<DragAndDrop>();

        // --- Wire ActionBar (vive en el propio panel) ---
        var actionBar = actionBarPanel.AddComponent<ActionBar>();
        SetPrivateField(actionBar, "_player", playerGo);
        SetPrivateField(actionBar, "_dragAndDrop", dragAndDrop);
        SetPrivateField(actionBar, "_itemIcons", barIcons);
        SetPrivateField(actionBar, "_defaultActionItem", emptyHands);

        // --- Wire Inventory (vive en el Player, para GetComponentInChildren<Inventory>()) ---
        var inventory = playerGo.GetComponent<Inventory>();
        SetPrivateField(inventory, "_itemIcons", invIcons);
        SetPrivateField(inventory, "_itemIconQuantities", invQuantities);
        SetPrivateField(inventory, "_dragAndDrop", dragAndDrop);

        // --- Wire PlayerActions ---
        var playerActions = playerGo.GetComponent<PlayerActions>();
        SetPrivateField(playerActions, "_actionBar", actionBar);

        // --- Enganchar cada slot de UI con su contenedor (esto no venia en el repo original) ---
        for (int i = 0; i < invSlotGos.Length; i++)
        {
            var slotUi = invSlotGos[i].AddComponent<InventorySlotUI>();
            slotUi.containerBehaviour = inventory;
            slotUi.slotIndex = i;
            slotUi.dragAndDrop = dragAndDrop;
        }
        for (int i = 0; i < barSlotGos.Length; i++)
        {
            var slotUi = barSlotGos[i].AddComponent<InventorySlotUI>();
            slotUi.containerBehaviour = actionBar;
            slotUi.slotIndex = i;
            slotUi.dragAndDrop = dragAndDrop;
        }

        // --- Bootstrap: carga un par de items al arrancar para ver algo enseguida ---
        var bootstrap = playerGo.AddComponent<InventoryDemoBootstrap>();
        bootstrap.inventory = inventory;
        bootstrap.starterItems = new Item[] { wood, potion };
        bootstrap.starterQuantities = new int[] { 5, 1 };

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("Escena de demo del inventario creada en " + ScenePath + ". Dale Play y probá: arrastrá 'Madera' o 'Pocion' del panel de arriba a la barra de abajo, y apretá 1/2/3 para equipar/desequipar (mirá la Console).");
    }

    private static GameObject CreateUIPanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        return go;
    }

    private static GameObject CreateSlot(Transform parent, string name, out Image icon, out Text quantityText, bool withQuantity = true)
    {
        var slotGo = new GameObject(name, typeof(RectTransform), typeof(Image));
        slotGo.transform.SetParent(parent, false);
        var slotRect = slotGo.GetComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(48, 48);
        var slotBg = slotGo.GetComponent<Image>();
        slotBg.color = new Color(1, 1, 1, 0.15f); // fondo del slot, siempre visible

        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(slotGo.transform, false);
        var iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(4, 4);
        iconRect.offsetMax = new Vector2(-4, -4);
        icon = iconGo.GetComponent<Image>();
        icon.color = new Color(0.9f, 0.7f, 0.2f); // color visible cuando el slot tiene algo
        icon.enabled = false; // el codigo (ActionBar/Inventory) lo prende cuando hay item

        quantityText = null;
        if (withQuantity)
        {
            var textGo = new GameObject("Quantity", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(slotGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(1, 0);
            textRect.anchoredPosition = new Vector2(-2, 2);
            textRect.sizeDelta = new Vector2(24, 16);
            quantityText = textGo.GetComponent<Text>();
            quantityText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            quantityText.fontSize = 12;
            quantityText.alignment = TextAnchor.LowerRight;
            quantityText.color = Color.white;
        }

        return slotGo;
    }

    private static DemoActionItem CreateDemoActionItem(string assetName, string displayName, bool stackable)
    {
        var path = $"{ItemsFolder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DemoActionItem>(path);
        if (existing != null) return existing;

        var item = ScriptableObject.CreateInstance<DemoActionItem>();
        var so = new SerializedObject(item);
        so.FindProperty("_name").stringValue = displayName;
        so.FindProperty("_isStackable").boolValue = stackable;
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static ResourceItem CreateResourceItem(string assetName, string displayName)
    {
        var path = $"{ItemsFolder}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<ResourceItem>(path);
        if (existing != null) return existing;

        var item = ScriptableObject.CreateInstance<ResourceItem>();
        var so = new SerializedObject(item);
        so.FindProperty("_name").stringValue = displayName;
        so.FindProperty("_isStackable").boolValue = true;
        so.FindProperty("_type").enumValueIndex = 0; // ResourceType.Wood
        so.ApplyModifiedPropertiesWithoutUndo();

        AssetDatabase.CreateAsset(item, path);
        return item;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var type = target.GetType();
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (field == null)
        {
            Debug.LogError($"No se encontro el campo '{fieldName}' en {type.Name}");
            return;
        }
        field.SetValue(target, value);
    }
}
