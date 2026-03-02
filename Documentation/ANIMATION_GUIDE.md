# Last Day - Animation & Pathfinding Complete Guide

## Table of Contents
1. [Animation System Overview](#1-animation-system-overview)
2. [Complete Character Controller with Pathfinding](#2-complete-character-controller)
3. [A* Pathfinding Setup](#3-a-pathfinding-setup)
4. [Sprite Sheet Generation Guide](#4-sprite-sheet-generation-guide)
5. [Unity Setup Checklist](#5-unity-setup-checklist)

---

## 1. Animation System Overview

### What You Need

```
ROBERT (Player Character)
═════════════════════════

SPRITE SHEETS NEEDED:
├── robert_idle.png (96×192) - 4 directions × 3 frames
├── robert_walk.png (128×192) - 4 directions × 4 frames
└── robert_portrait.png (96×64) - 3 expressions

MARTHA (NPC)
════════════

SPRITE SHEETS NEEDED:
├── martha_idle.png (96×144) - 3 directions × 3 frames (no back view needed)
├── martha_portrait.png (128×64) - 4 expressions
└── (no walk animation - she stays in place)

ANIMATION SPEEDS:
├── Idle: 0.4-0.6 seconds per frame (SLOW - elderly, subtle)
├── Walk: 0.15-0.2 seconds per frame (SLOW - elderly movement)
└── Portrait blink: every 3-5 seconds randomly
```

### Frame-by-Frame Breakdown

```
WALK CYCLE - FRONT VIEW (4 frames)
══════════════════════════════════

Frame 1: Right foot forward    Frame 2: Feet passing
      ██                             ██
     ████                           ████
      ██                             ██
     ████                           ████
    ██  ██                          ████
   █      █                         ████

Frame 3: Left foot forward     Frame 4: Feet passing
      ██                             ██
     ████                           ████
      ██                             ██
     ████                           ████
   ██  ██                           ████
  █      █                          ████

FOR ELDERLY CHARACTER:
- Slightly hunched shoulders (top of sprite shifted down)
- Smaller step width
- Optional: cane in one hand
- Slower animation speed

IDLE CYCLE - BREATHING (3 frames)
═════════════════════════════════

Frame 1: Normal         Frame 2: Inhale        Frame 3: Exhale
      ██                     ██                     ██
     ████                   █████                  ████
      ██                     ███                    █
     ████                   █████                  ████
     ████                    ████                  ████
     █  █                    █  █                  █  █

Just 1-2 pixel difference in chest area
Very subtle - player shouldn't consciously notice
```

---

## 2. Complete Character Controller

### 2.1 Player Controller with Pathfinding

```csharp
// PlayerController2D.cs
using UnityEngine;
using System.Collections.Generic;

public class PlayerController2D : MonoBehaviour
{
    public static PlayerController2D Instance { get; private set; }
    
    [Header("Movement")]
    public float moveSpeed = 2f;  // Slow for elderly character
    public float pathNodeReachDistance = 0.1f;
    
    [Header("Components")]
    public SpriteRenderer spriteRenderer;
    public Animator animator;
    
    [Header("Pathfinding")]
    public SimplePathfinder pathfinder;  // Reference to pathfinder
    
    [Header("Animation")]
    public SubtleIdleMovement idleMovement;
    
    // State
    private List<Vector2> currentPath;
    private int currentPathIndex;
    private bool isMoving;
    private Vector2 facingDirection = Vector2.down;
    private System.Action onReachDestination;
    
    // Animator hashes
    private static readonly int IsWalking = Animator.StringToHash("IsWalking");
    private static readonly int DirectionX = Animator.StringToHash("DirectionX");
    private static readonly int DirectionY = Animator.StringToHash("DirectionY");
    
    void Awake()
    {
        Instance = this;
    }
    
    void Update()
    {
        if (!isMoving) return;
        
        if (currentPath == null || currentPathIndex >= currentPath.Count)
        {
            StopMoving();
            return;
        }
        
        // Get current target node
        Vector2 targetPos = currentPath[currentPathIndex];
        Vector2 currentPos = transform.position;
        Vector2 direction = (targetPos - currentPos).normalized;
        
        // Move towards target
        transform.position += (Vector3)(direction * moveSpeed * Time.deltaTime);
        
        // Update animation
        UpdateAnimation(direction);
        
        // Check if reached current node
        if (Vector2.Distance(currentPos, targetPos) < pathNodeReachDistance)
        {
            currentPathIndex++;
            
            // Check if reached final destination
            if (currentPathIndex >= currentPath.Count)
            {
                StopMoving();
            }
        }
    }
    
    /// <summary>
    /// Move to a world position using pathfinding
    /// </summary>
    public void MoveTo(Vector2 destination, System.Action onComplete = null)
    {
        // Don't move if in dialogue
        if (GameStateMachine.Instance?.CurrentState == GameState.InDialogue)
        {
            return;
        }
        
        // Find path
        currentPath = pathfinder.FindPath(transform.position, destination);
        
        if (currentPath == null || currentPath.Count == 0)
        {
            Debug.Log("No path found to destination");
            return;
        }
        
        currentPathIndex = 0;
        isMoving = true;
        onReachDestination = onComplete;
        
        // Disable idle movement while walking
        if (idleMovement != null)
            idleMovement.enabled = false;
        
        // Set animator to walking
        animator.SetBool(IsWalking, true);
    }
    
    /// <summary>
    /// Move to an object, then interact with it
    /// </summary>
    public void MoveToAndInteract(InteractableObject2D target)
    {
        // Calculate interaction point (slightly in front of object)
        Vector2 interactionPoint = CalculateInteractionPoint(target.transform.position);
        
        MoveTo(interactionPoint, () => {
            // Face the object
            FacePosition(target.transform.position);
            // Trigger interaction
            target.OnInteract();
        });
    }
    
    Vector2 CalculateInteractionPoint(Vector2 objectPos)
    {
        // Stand below the object (so player faces up at it)
        Vector2 point = objectPos;
        point.y -= 0.5f;
        
        // Could be smarter - find nearest walkable point to object
        return point;
    }
    
    void UpdateAnimation(Vector2 direction)
    {
        // Determine primary direction (4-way)
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            // Horizontal movement
            animator.SetFloat(DirectionX, Mathf.Sign(direction.x));
            animator.SetFloat(DirectionY, 0);
            facingDirection = new Vector2(Mathf.Sign(direction.x), 0);
        }
        else
        {
            // Vertical movement
            animator.SetFloat(DirectionX, 0);
            animator.SetFloat(DirectionY, Mathf.Sign(direction.y));
            facingDirection = new Vector2(0, Mathf.Sign(direction.y));
        }
        
        // Flip sprite for left/right (if using single side sprite)
        // Uncomment if your sprite sheet only has right-facing walk:
        // spriteRenderer.flipX = direction.x < 0;
    }
    
    void FacePosition(Vector2 pos)
    {
        Vector2 dir = (pos - (Vector2)transform.position).normalized;
        
        // Set facing direction for idle animation
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            animator.SetFloat(DirectionX, Mathf.Sign(dir.x));
            animator.SetFloat(DirectionY, 0);
        }
        else
        {
            animator.SetFloat(DirectionX, 0);
            animator.SetFloat(DirectionY, Mathf.Sign(dir.y));
        }
    }
    
    void StopMoving()
    {
        isMoving = false;
        currentPath = null;
        
        // Re-enable idle movement
        if (idleMovement != null)
            idleMovement.enabled = true;
        
        // Set animator to idle
        animator.SetBool(IsWalking, false);
        
        // Invoke callback
        onReachDestination?.Invoke();
        onReachDestination = null;
    }
    
    public void ForceStop()
    {
        StopMoving();
    }
    
    public bool IsCurrentlyMoving => isMoving;
    public Vector2 FacingDirection => facingDirection;
}
```

### 2.2 Subtle Idle Movement (Breathing/Sway)

```csharp
// SubtleIdleMovement.cs
using UnityEngine;

public class SubtleIdleMovement : MonoBehaviour
{
    [Header("Target Transform")]
    public Transform characterSprite;  // The actual sprite, not the parent
    
    [Header("Breathing")]
    [Tooltip("How much the chest rises and falls")]
    public float breathingScaleAmount = 0.015f;  // Very subtle
    public float breathingSpeed = 0.4f;  // Slow, elderly breathing
    
    [Header("Sway")]
    [Tooltip("Gentle side-to-side sway")]
    public float swayAmount = 0.003f;  // Almost imperceptible
    public float swaySpeed = 0.2f;
    
    [Header("Occasional Movements (Elderly Feel)")]
    public bool enableOccasionalTremor = true;
    public float tremorChance = 0.1f;  // Per second
    public float tremorAmount = 0.01f;
    public float tremorDuration = 0.3f;
    
    [Header("Portrait Mode (for dialogue)")]
    public bool isPortrait = false;
    public float blinkInterval = 4f;  // Seconds between blinks
    
    // State
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private float nextTremorTime;
    private float tremorEndTime;
    private bool isTremoring;
    private float nextBlinkTime;
    
    void Start()
    {
        if (characterSprite == null)
            characterSprite = transform;
            
        originalScale = characterSprite.localScale;
        originalPosition = characterSprite.localPosition;
        
        nextTremorTime = Time.time + Random.Range(2f, 5f);
        nextBlinkTime = Time.time + Random.Range(blinkInterval * 0.5f, blinkInterval);
    }
    
    void Update()
    {
        if (characterSprite == null) return;
        
        Vector3 newScale = originalScale;
        Vector3 newPosition = originalPosition;
        
        // Breathing - subtle Y scale pulse
        float breathPhase = Time.time * breathingSpeed * Mathf.PI * 2;
        float breathOffset = Mathf.Sin(breathPhase) * breathingScaleAmount;
        newScale.y = originalScale.y + breathOffset;
        
        // Very subtle position shift with breathing
        newPosition.y = originalPosition.y + breathOffset * 0.5f;
        
        // Gentle sway
        float swayPhase = Time.time * swaySpeed * Mathf.PI * 2;
        float swayOffset = Mathf.Sin(swayPhase) * swayAmount;
        newPosition.x = originalPosition.x + swayOffset;
        
        // Occasional tremor (elderly feel)
        if (enableOccasionalTremor)
        {
            UpdateTremor(ref newPosition);
        }
        
        // Apply
        characterSprite.localScale = newScale;
        characterSprite.localPosition = newPosition;
        
        // Portrait blinking (if this is a portrait)
        if (isPortrait)
        {
            UpdateBlink();
        }
    }
    
    void UpdateTremor(ref Vector3 position)
    {
        // Start new tremor
        if (!isTremoring && Time.time > nextTremorTime)
        {
            if (Random.value < tremorChance * Time.deltaTime)
            {
                isTremoring = true;
                tremorEndTime = Time.time + tremorDuration;
                nextTremorTime = Time.time + Random.Range(3f, 8f);
            }
        }
        
        // Apply tremor
        if (isTremoring)
        {
            float tremorX = Mathf.Sin(Time.time * 40f) * tremorAmount;
            tremorX += Mathf.Sin(Time.time * 55f) * tremorAmount * 0.5f;
            position.x += tremorX;
            
            if (Time.time > tremorEndTime)
            {
                isTremoring = false;
            }
        }
    }
    
    void UpdateBlink()
    {
        // This would trigger a blink animation
        // Requires the portrait animator to have a "Blink" trigger
        if (Time.time > nextBlinkTime)
        {
            // GetComponent<Animator>()?.SetTrigger("Blink");
            nextBlinkTime = Time.time + blinkInterval + Random.Range(-1f, 1f);
        }
    }
    
    // Call this when character starts walking
    public void OnStartWalking()
    {
        enabled = false;
        // Reset to original position/scale
        if (characterSprite != null)
        {
            characterSprite.localScale = originalScale;
            characterSprite.localPosition = originalPosition;
        }
    }
    
    // Call this when character stops walking
    public void OnStopWalking()
    {
        enabled = true;
    }
}
```

---

## 3. A* Pathfinding Setup

### 3.1 Simple Grid-Based Pathfinder

```csharp
// SimplePathfinder.cs
using UnityEngine;
using System.Collections.Generic;

public class SimplePathfinder : MonoBehaviour
{
    [Header("Grid Settings")]
    public Vector2 gridOrigin = new Vector2(-5, -3);  // Bottom-left of room
    public Vector2 gridSize = new Vector2(10, 6);     // Room dimensions
    public float cellSize = 0.5f;                      // Grid resolution
    
    [Header("Obstacles")]
    public LayerMask obstacleLayer;
    public float obstacleCheckRadius = 0.2f;
    
    [Header("Debug")]
    public bool showDebugGrid = true;
    public bool showPath = true;
    
    // Grid data
    private bool[,] walkableGrid;
    private int gridWidth;
    private int gridHeight;
    
    void Start()
    {
        BuildGrid();
    }
    
    /// <summary>
    /// Scan the room and build walkable grid
    /// </summary>
    public void BuildGrid()
    {
        gridWidth = Mathf.CeilToInt(gridSize.x / cellSize);
        gridHeight = Mathf.CeilToInt(gridSize.y / cellSize);
        walkableGrid = new bool[gridWidth, gridHeight];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = GridToWorld(x, y);
                
                // Check if this cell is blocked by an obstacle
                Collider2D obstacle = Physics2D.OverlapCircle(worldPos, obstacleCheckRadius, obstacleLayer);
                walkableGrid[x, y] = (obstacle == null);
            }
        }
        
        Debug.Log($"Pathfinding grid built: {gridWidth}x{gridHeight} cells");
    }
    
    /// <summary>
    /// Find a path from start to end
    /// </summary>
    public List<Vector2> FindPath(Vector2 start, Vector2 end)
    {
        // Convert world positions to grid coordinates
        Vector2Int startGrid = WorldToGrid(start);
        Vector2Int endGrid = WorldToGrid(end);
        
        // Validate positions
        if (!IsValidGridPos(startGrid) || !IsValidGridPos(endGrid))
        {
            Debug.LogWarning("Start or end position outside grid");
            return null;
        }
        
        if (!walkableGrid[endGrid.x, endGrid.y])
        {
            // Find nearest walkable cell to destination
            endGrid = FindNearestWalkable(endGrid);
            if (endGrid.x < 0)
            {
                Debug.LogWarning("No walkable cell near destination");
                return null;
            }
        }
        
        // A* algorithm
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector2Int>();
        var nodeMap = new Dictionary<Vector2Int, PathNode>();
        
        var startNode = new PathNode(startGrid, null, 0, Heuristic(startGrid, endGrid));
        openSet.Add(startNode);
        nodeMap[startGrid] = startNode;
        
        while (openSet.Count > 0)
        {
            // Get node with lowest F cost
            PathNode current = GetLowestFCost(openSet);
            
            // Found the goal
            if (current.Position == endGrid)
            {
                return ReconstructPath(current);
            }
            
            openSet.Remove(current);
            closedSet.Add(current.Position);
            
            // Check neighbors (4-directional)
            foreach (var neighborPos in GetNeighbors(current.Position))
            {
                if (closedSet.Contains(neighborPos))
                    continue;
                    
                if (!IsValidGridPos(neighborPos) || !walkableGrid[neighborPos.x, neighborPos.y])
                    continue;
                
                float tentativeG = current.G + cellSize;
                
                PathNode neighborNode;
                if (!nodeMap.TryGetValue(neighborPos, out neighborNode))
                {
                    neighborNode = new PathNode(neighborPos, current, tentativeG, Heuristic(neighborPos, endGrid));
                    nodeMap[neighborPos] = neighborNode;
                    openSet.Add(neighborNode);
                }
                else if (tentativeG < neighborNode.G)
                {
                    neighborNode.Parent = current;
                    neighborNode.G = tentativeG;
                    neighborNode.F = tentativeG + neighborNode.H;
                }
            }
        }
        
        Debug.LogWarning("No path found");
        return null;
    }
    
    // Helper: A* node
    private class PathNode
    {
        public Vector2Int Position;
        public PathNode Parent;
        public float G;  // Cost from start
        public float H;  // Heuristic to end
        public float F;  // Total cost
        
        public PathNode(Vector2Int pos, PathNode parent, float g, float h)
        {
            Position = pos;
            Parent = parent;
            G = g;
            H = h;
            F = g + h;
        }
    }
    
    private float Heuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    private PathNode GetLowestFCost(List<PathNode> nodes)
    {
        PathNode lowest = nodes[0];
        for (int i = 1; i < nodes.Count; i++)
        {
            if (nodes[i].F < lowest.F)
                lowest = nodes[i];
        }
        return lowest;
    }
    
    private List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(pos.x + 1, pos.y),
            new Vector2Int(pos.x - 1, pos.y),
            new Vector2Int(pos.x, pos.y + 1),
            new Vector2Int(pos.x, pos.y - 1)
        };
    }
    
    private List<Vector2> ReconstructPath(PathNode endNode)
    {
        var path = new List<Vector2>();
        var current = endNode;
        
        while (current != null)
        {
            path.Add(GridToWorld(current.Position.x, current.Position.y));
            current = current.Parent;
        }
        
        path.Reverse();
        
        // Optional: Smooth the path
        path = SmoothPath(path);
        
        return path;
    }
    
    private List<Vector2> SmoothPath(List<Vector2> path)
    {
        // Simple path smoothing - remove unnecessary waypoints
        if (path.Count <= 2) return path;
        
        var smoothed = new List<Vector2> { path[0] };
        
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2 prev = path[i - 1];
            Vector2 curr = path[i];
            Vector2 next = path[i + 1];
            
            // Keep point if direction changes
            Vector2 dir1 = (curr - prev).normalized;
            Vector2 dir2 = (next - curr).normalized;
            
            if (Vector2.Dot(dir1, dir2) < 0.99f)
            {
                smoothed.Add(curr);
            }
        }
        
        smoothed.Add(path[path.Count - 1]);
        return smoothed;
    }
    
    private Vector2Int FindNearestWalkable(Vector2Int pos)
    {
        int searchRadius = 1;
        int maxRadius = Mathf.Max(gridWidth, gridHeight);
        
        while (searchRadius < maxRadius)
        {
            for (int x = -searchRadius; x <= searchRadius; x++)
            {
                for (int y = -searchRadius; y <= searchRadius; y++)
                {
                    Vector2Int check = new Vector2Int(pos.x + x, pos.y + y);
                    if (IsValidGridPos(check) && walkableGrid[check.x, check.y])
                    {
                        return check;
                    }
                }
            }
            searchRadius++;
        }
        
        return new Vector2Int(-1, -1);
    }
    
    // Conversion helpers
    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - gridOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - gridOrigin.y) / cellSize);
        return new Vector2Int(x, y);
    }
    
    public Vector2 GridToWorld(int x, int y)
    {
        return new Vector2(
            gridOrigin.x + (x + 0.5f) * cellSize,
            gridOrigin.y + (y + 0.5f) * cellSize
        );
    }
    
    public bool IsValidGridPos(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth && pos.y >= 0 && pos.y < gridHeight;
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (!showDebugGrid) return;
        
        // Draw grid bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            gridOrigin + gridSize * 0.5f,
            gridSize
        );
        
        // Draw walkable cells
        if (walkableGrid == null) return;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2 worldPos = GridToWorld(x, y);
                Gizmos.color = walkableGrid[x, y] ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
                Gizmos.DrawCube(worldPos, Vector3.one * cellSize * 0.8f);
            }
        }
    }
}
```

### 3.2 Click Handler Integration

```csharp
// ClickToMoveHandler.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToMoveHandler : MonoBehaviour
{
    [Header("Layers")]
    public LayerMask interactableLayer;
    public LayerMask walkableLayer;
    
    [Header("Feedback")]
    public GameObject clickIndicatorPrefab;  // Optional: shows where you clicked
    
    private GameObject currentIndicator;
    
    void Update()
    {
        // Don't process clicks if over UI or in dialogue
        if (EventSystem.current.IsPointerOverGameObject())
            return;
            
        if (GameStateMachine.Instance?.CurrentState == GameState.InDialogue)
            return;
        
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }
    
    void HandleClick()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        
        // First check: Did we click an interactable?
        Collider2D interactableHit = Physics2D.OverlapPoint(mouseWorldPos, interactableLayer);
        if (interactableHit != null)
        {
            var interactable = interactableHit.GetComponent<InteractableObject2D>();
            if (interactable != null)
            {
                PlayerController2D.Instance.MoveToAndInteract(interactable);
                ShowClickIndicator(interactable.transform.position);
                return;
            }
        }
        
        // Second check: Did we click a walkable area?
        Collider2D walkableHit = Physics2D.OverlapPoint(mouseWorldPos, walkableLayer);
        if (walkableHit != null)
        {
            PlayerController2D.Instance.MoveTo(mouseWorldPos);
            ShowClickIndicator(mouseWorldPos);
        }
    }
    
    void ShowClickIndicator(Vector2 position)
    {
        if (clickIndicatorPrefab == null) return;
        
        // Destroy old indicator
        if (currentIndicator != null)
            Destroy(currentIndicator);
        
        // Create new indicator
        currentIndicator = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);
        Destroy(currentIndicator, 0.5f);  // Auto-destroy after 0.5 seconds
    }
}
```

---

## 4. Sprite Sheet Generation Guide

### 4.1 Using LPC Character Generator

```
RECOMMENDED APPROACH: LPC Character Generator

URL: https://sanderfrenken.github.io/Universal-LPC-Spritesheet-Character-Generator/

STEP BY STEP:

1. Open the generator

2. Configure Robert (protagonist):
   - Body: Light/Medium skin
   - Hair: Gray, short or balding
   - Facial Hair: Optional gray beard
   - Clothes: 
     - Shirt: Cardigan or button-up
     - Pants: Slacks
   - NO: Armor, weapons, fantasy elements

3. Configure Martha (NPC):
   - Body: Light/Medium skin
   - Hair: Gray, styled (bun or waves)
   - Clothes:
     - Dress or blouse + skirt
     - Cardigan
   - Accessories: Optional glasses

4. Export Options:
   - Download "Universal" sheet (includes all animations)
   - Or download individual rows (idle, walk)
   
5. The generator creates a 832×1344 sprite sheet with:
   - 13 columns (animation frames)
   - 21 rows (different animations x 4 directions)
   - Each cell: 64×64 pixels

6. POST-PROCESSING:
   - The default is 64x64 which is larger than needed
   - Scale down to 32x48 in image editor if desired
   - Or keep 64x64 and adjust Pixels Per Unit in Unity
```

### 4.2 Manual Pixel Art (if doing yourself)

```
COLOR PALETTE (for consistency):

SKIN TONES (elderly):
- Base: #E8C4A8
- Shadow: #C9A080  
- Highlight: #F5DCC8

GRAY HAIR:
- Base: #A0A0A0
- Shadow: #707070
- Highlight: #C8C8C8

ROBERT'S CLOTHES:
- Cardigan Base: #6B5344
- Cardigan Shadow: #4A3A2F
- Pants: #3D3D4A

MARTHA'S CLOTHES:
- Dress Base: #7A6B8C
- Dress Shadow: #5A4D6A
- Cardigan: #8B7355

TOOLS:
- Aseprite ($20) - Best for animation
- Piskel (free) - Browser-based
- Libresprite (free) - Aseprite fork
```

### 4.3 Importing Sprite Sheets to Unity

```csharp
/*
UNITY SPRITE SHEET IMPORT CHECKLIST:

1. IMPORT SETTINGS:
   - Texture Type: Sprite (2D and UI)
   - Sprite Mode: Multiple
   - Pixels Per Unit: 32 (or 64 if using LPC default)
   - Filter Mode: Point (no filter) ← CRITICAL
   - Compression: None

2. SLICE THE SHEET:
   - Click "Sprite Editor"
   - Slice > Grid By Cell Size
   - Cell Size: 64×64 (LPC) or 32×48 (custom)
   - Click Slice, then Apply

3. NAME YOUR SPRITES:
   After slicing, rename sprites to:
   - robert_walk_down_0, robert_walk_down_1, etc.
   - robert_idle_down_0, robert_idle_down_1, etc.
   
4. CREATE ANIMATION CLIPS:
   - Window > Animation > Animation
   - Select Robert GameObject
   - Create New Clip "Robert_Walk_Down"
   - Drag sprites into timeline
   - Set Samples: 8 (for walk) or 3 (for idle)

5. CREATE ANIMATOR CONTROLLER:
   - Right-click Project > Create > Animator Controller
   - Name: "RobertAnimator"
   - Open Animator window
   - Add parameters: IsWalking (bool), DirectionX (float), DirectionY (float)
   - Create Blend Trees for walk and idle
*/
```

---

## 5. Unity Setup Checklist

```
□ PROJECT SETUP
  □ Create Unity 2D project
  □ Import TextMeshPro
  □ Import 2D Pixel Perfect package
  □ Set Edit > Project Settings > Editor > Default Behavior Mode: 2D

□ CAMERA SETUP
  □ Main Camera: Orthographic
  □ Add Pixel Perfect Camera component
  □ Assets PPU: 32 (or 64)
  □ Reference Resolution: 480×270
  □ Upscale Render Texture: ON

□ LAYERS
  □ Create Layer: Walkable
  □ Create Layer: Obstacles
  □ Create Layer: Interactables
  □ Create Layer: Characters

□ IMPORT ART
  □ Import character sprite sheets
  □ Set Filter Mode: Point (no filter)
  □ Set Compression: None
  □ Slice sprite sheets
  □ Create animation clips

□ SCENE HIERARCHY
  □ GameManager (empty + scripts)
  □ Pathfinder (empty + SimplePathfinder)
  □ Background (sprite)
  □ Player (sprite + controller + animator)
  □ Martha (sprite + idle script)
  □ Interactables (child objects)
  □ Canvas (UI)

□ PLAYER SETUP
  □ Add SpriteRenderer
  □ Add Animator + Controller
  □ Add PlayerController2D script
  □ Add SubtleIdleMovement script
  □ Add Collider2D (trigger, for interaction areas)
  □ Set Layer: Characters
  □ Set Sorting Layer: Characters

□ PATHFINDING SETUP
  □ Add SimplePathfinder to scene
  □ Configure grid origin and size to match room
  □ Create obstacles with Collider2D on Obstacles layer
  □ Test: Play mode, click around, watch debug grid

□ INTERACTABLES SETUP
  □ Each object: Sprite + Collider2D + InteractableObject2D
  □ Set Layer: Interactables
  □ Configure object ID, memory ID
  □ Add glow sprite child (for hover effect)

□ AUDIO SETUP
  □ Create AudioManager
  □ Import music files
  □ Set Music: Load Type Streaming
  □ Test ambient music loop point
```

---

## Quick Reference: Animation Speeds

```
ELDERLY CHARACTER ANIMATION TIMINGS:

Walk Animation:
- 4 frames
- 0.15-0.2 seconds per frame
- Full cycle: 0.6-0.8 seconds
- This feels slow/deliberate for elderly

Idle Animation:
- 3 frames  
- 0.4-0.6 seconds per frame
- Full cycle: 1.2-1.8 seconds
- Barely noticeable breathing

Code-Based Subtle Movement:
- Breathing scale: 0.015 (1.5% height change)
- Breathing speed: 0.4 Hz (one breath per 2.5 seconds)
- Sway amount: 0.003 units
- Sway speed: 0.2 Hz (one sway per 5 seconds)

Portrait Blink:
- Every 3-5 seconds
- Blink duration: 0.1 seconds (instant)
```
