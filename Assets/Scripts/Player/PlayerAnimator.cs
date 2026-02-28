using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] walkUp;
    [SerializeField] private Sprite[] walkDown;
    [SerializeField] private Sprite[] walkLeft;
    [SerializeField] private Sprite[] walkRight;
    [SerializeField] private Sprite idleUp;
    [SerializeField] private Sprite idleDown;
    [SerializeField] private Sprite idleLeft;
    [SerializeField] private Sprite idleRight;
    [SerializeField] private float frameRate = 8f;

    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private float frameTimer;
    private int currentFrame;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (playerController == null || spriteRenderer == null) return;

        if (playerController.IsMoving)
        {
            AnimateWalk();
        }
        else
        {
            ShowIdle();
        }
    }

    private void AnimateWalk()
    {
        var frames = GetWalkFrames(playerController.FacingDirection);
        if (frames == null || frames.Length == 0) return;

        frameTimer += Time.deltaTime * frameRate;
        if (frameTimer >= 1f)
        {
            frameTimer -= 1f;
            currentFrame = (currentFrame + 1) % frames.Length;
        }

        spriteRenderer.sprite = frames[currentFrame];
    }

    private void ShowIdle()
    {
        currentFrame = 0;
        frameTimer = 0f;

        spriteRenderer.sprite = playerController.FacingDirection switch
        {
            Direction.Up => idleUp,
            Direction.Down => idleDown,
            Direction.Left => idleLeft,
            Direction.Right => idleRight,
            _ => idleDown
        };
    }

    private Sprite[] GetWalkFrames(Direction dir)
    {
        return dir switch
        {
            Direction.Up => walkUp,
            Direction.Down => walkDown,
            Direction.Left => walkLeft,
            Direction.Right => walkRight,
            _ => walkDown
        };
    }
}
