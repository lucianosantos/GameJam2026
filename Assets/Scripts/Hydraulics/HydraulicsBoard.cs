using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using System.Collections;

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
        private readonly HydraulicsTileView[] tiles =
            new HydraulicsTileView[HydraulicsPuzzle.Columns * HydraulicsPuzzle.Rows];
        private HydraulicsPuzzle puzzle;
        private GridLayoutGroup grid;
        private CanvasGroup interactionGroup;
        private const float RotationDuration = 0.3f;
        private Coroutine rotationRoutine;
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
            CancelRotation();
            if (cameraController != null) cameraController.ViewChanged -= RefreshInteraction;
            interactionGroup.interactable = false;
            interactionGroup.blocksRaycasts = false;
        }

        private void RefreshInteraction()
        {
            bool focused = cameraController != null && cameraController.IsFocusedOn(cameraTarget);
            interactionGroup.interactable = focused && rotationRoutine == null;
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
                tiles[i].name = $"Pipe_{i % HydraulicsPuzzle.Columns}_{i / HydraulicsPuzzle.Columns}";
                tiles[i].Initialize(this, i);
            }
            ResizeCells();
            RestartPuzzle();
        }

        public void RestartPuzzle()
        {
            if (grid == null) return;
            CancelRotation();
            int seed = Random.Range(0, int.MaxValue);
            puzzle = new HydraulicsPuzzle(seed);
            puzzle.Shuffle(seed);
            RefreshTiles();
            RefreshInteraction();
        }

        public void RotateTile(int index)
        {
            if (!isActiveAndEnabled || cameraController == null ||
                !cameraController.IsFocusedOn(cameraTarget) ||
                puzzle == null || rotationRoutine != null || index < 0 || index >= tiles.Length ||
                puzzle.IsSolved || puzzle.IsLocked(index)) return;
            rotationRoutine = StartCoroutine(RotateAndEvaluate(index));
            RefreshInteraction();
        }

        private IEnumerator RotateAndEvaluate(int index)
        {
            yield return tiles[index].AnimateClockwise(RotationDuration);
            rotationRoutine = null;
            bool rotated = puzzle.Rotate(index);
            RefreshTiles();
            RefreshInteraction();
            if (rotated && puzzle.IsSolved) onCompleted.Invoke();
        }

        private void CancelRotation()
        {
            if (rotationRoutine == null) return;
            StopCoroutine(rotationRoutine);
            rotationRoutine = null;
            // A cancelled animation does not change the logical orientation.
            if (puzzle != null) RefreshTiles();
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
            float width = (rect.width - grid.padding.horizontal -
                grid.spacing.x * (HydraulicsPuzzle.Columns - 1)) / HydraulicsPuzzle.Columns;
            float height = (rect.height - grid.padding.vertical -
                grid.spacing.y * (HydraulicsPuzzle.Rows - 1)) / HydraulicsPuzzle.Rows;
            // World-space canvases may use sub-unit RectTransforms. Do not force a
            // one-unit minimum, which would make the grid larger than its container.
            grid.cellSize = Vector2.one * Mathf.Max(0, Mathf.Min(width, height));
        }
    }
}
