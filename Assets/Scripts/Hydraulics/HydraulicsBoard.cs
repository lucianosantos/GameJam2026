using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameJam2026.Hydraulics
{
    [RequireComponent(typeof(GridLayoutGroup))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class HydraulicsBoard : MonoBehaviour
    {
        [SerializeField] private HydraulicsTileView tilePrefab;
        [Header("Straight: North/South. Elbow: East/South.")]
        [SerializeField] private Sprite straightDry;
        [SerializeField] private Sprite straightWet;
        [SerializeField] private Sprite elbowDry;
        [SerializeField] private Sprite elbowWet;
        [Header("Interaction is enabled only at this camera target")]
        [SerializeField] private CameraController cameraController;
        [SerializeField] private Transform cameraTarget;
        [Header("Fixed inlet (top-left) and outlet (bottom-right)")]
        [SerializeField] private Color inletColor = new Color(0.4f, 0.8f, 1f);
        [SerializeField] private Color outletColor = new Color(1f, 0.8f, 0.35f);
        [SerializeField] private UnityEvent onCompleted = new UnityEvent();
        private readonly HydraulicsTileView[] tiles = new HydraulicsTileView[24];
        private HydraulicsPuzzle puzzle;
        private GridLayoutGroup grid;
        private CanvasGroup interactionGroup;
        public bool IsCompleted => puzzle != null && puzzle.IsSolved;
        public UnityEvent OnCompleted => onCompleted;

        private void Awake()
        {
            interactionGroup = GetComponent<CanvasGroup>();
            if (interactionGroup == null) interactionGroup = gameObject.AddComponent<CanvasGroup>();
            interactionGroup.interactable = false;
            interactionGroup.blocksRaycasts = false;
        }

        private void OnEnable()
        {
            if (cameraController != null) cameraController.ViewChanged += RefreshInteraction;
            RefreshInteraction();
        }

        private void OnDisable()
        {
            if (cameraController != null) cameraController.ViewChanged -= RefreshInteraction;
            interactionGroup.interactable = false;
            interactionGroup.blocksRaycasts = false;
        }

        private void RefreshInteraction()
        {
            bool focused = cameraController != null && cameraController.IsFocusedOn(cameraTarget);
            interactionGroup.interactable = focused;
            interactionGroup.blocksRaycasts = focused;
        }

        private void Start()
        {
            if (tilePrefab == null || !tilePrefab.IsConfigured || straightDry == null ||
                straightWet == null || elbowDry == null || elbowWet == null)
            {
                Debug.LogError("Hydraulics: assign a configured tile prefab and all four pipe sprites.", this);
                enabled = false;
                return;
            }
            // A camera configuration error blocks input, but must not hide the board.
            if (cameraController == null || cameraTarget == null)
            {
                Debug.LogWarning("Hydraulics: interaction needs Camera Controller and Camera Target. Assign CameraPositions/Hidraulics as Camera Target.", this);
            }
            else if (cameraTarget == cameraController.transform)
            {
                Debug.LogWarning("Hydraulics: interaction is blocked because Camera Target points to Main Camera. Stop Play Mode and assign CameraPositions/Hidraulics as Camera Target.", this);
            }
            grid = GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = HydraulicsPuzzle.Columns;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = Instantiate(tilePrefab, transform);
                tiles[i].name = $"Pipe_{i % 6}_{i / 6}";
                tiles[i].Initialize(this, i);
            }
            ResizeCells();
            RestartPuzzle();
        }

        public void RestartPuzzle()
        {
            if (grid == null) return;
            int seed = Random.Range(0, int.MaxValue);
            puzzle = new HydraulicsPuzzle(seed);
            puzzle.Shuffle(seed);
            RefreshTiles();
        }

        public void RotateTile(int index)
        {
            if (!isActiveAndEnabled || cameraController == null ||
                !cameraController.IsFocusedOn(cameraTarget) ||
                puzzle == null || !puzzle.Rotate(index)) return;
            RefreshTiles();
            if (puzzle.IsSolved) onCompleted.Invoke();
        }

        private void RefreshTiles()
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                bool wet = puzzle.IsWet(i);
                Sprite sprite = puzzle.IsElbow(i) ? (wet ? elbowWet : elbowDry) :
                    (wet ? straightWet : straightDry);
                Color color = i == 0 ? inletColor : i == tiles.Length - 1 ? outletColor : Color.white;
                // The straight's full length defines the common pixel scale for a cell.
                // The East/South elbow occupies the bottom-right of that same virtual tile.
                float cellPixels = straightDry.rect.height;
                Vector2 size = new Vector2(sprite.rect.width, sprite.rect.height) / cellPixels;
                Vector2 offset = puzzle.IsElbow(i)
                    ? new Vector2(1f - size.x, 0f)
                    : new Vector2((1f - size.x) * 0.5f, 0f);
                tiles[i].Render(sprite, puzzle.Rotation(i), puzzle.IsLocked(i) || puzzle.IsSolved,
                    color, new Rect(offset, size));
            }
        }

        private void OnRectTransformDimensionsChange() => ResizeCells();

        private void ResizeCells()
        {
            if (grid == null) return;
            Rect rect = ((RectTransform)transform).rect;
            float width = (rect.width - grid.padding.horizontal - grid.spacing.x * 5) / 6;
            float height = (rect.height - grid.padding.vertical - grid.spacing.y * 3) / 4;
            // World-space canvases may use sub-unit RectTransforms. Do not force a
            // one-unit minimum, which would make the grid larger than its container.
            grid.cellSize = Vector2.one * Mathf.Max(0, Mathf.Min(width, height));
        }
    }
}
