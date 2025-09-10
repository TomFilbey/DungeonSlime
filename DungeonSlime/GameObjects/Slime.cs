using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using DungeonSlime.Services;


namespace DungeonSlime.GameObjects;

public class Slime
{
    // A constant value that represents the amount of time to wait between
    // movement updates.
    private static readonly TimeSpan s_movementTime = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan s_mediumMovementTime = TimeSpan.FromMilliseconds(300); // Slower for medium (half speed)

    // The amount of time that has elapsed since the last movement update.
    private TimeSpan _movementTimer;

    // Normalized value (0-1) representing progress between movement ticks for visual interpolation
    private float _movementProgress;

    // The next direction to apply to the head of the slime chain during the
    // next movement update.
    private Vector2 _nextDirection;

    // The number of pixels to move the head segment during the movement cycle.
    private float _stride;

    // Tracks the segments of the slime chain.
    private List<SlimeSegment> _segments;

    // The AnimatedSprite used when drawing each slime segment
    private AnimatedSprite _sprite;

    // Buffer to queue inputs input by player during input polling.
    private Queue<Vector2> _inputBuffer;

    // The maximum size of the buffer queue.
    private const int MAX_BUFFER_SIZE = 2;

    // Reference to the difficulty service
    private SlimeDifficultyService _difficultyService;

    // For easy mode - direct position movement
    private Vector2 _easyModePosition;
    private bool _isEasyMode = false;

    /// <summary>
    /// Event that is raised if it is detected that the head segment of the slime
    /// has collided with a body segment.
    /// </summary>
    public event EventHandler BodyCollision;

    /// <summary>
    /// Creates a new Slime using the specified animated sprite.
    /// </summary>
    /// <param name="sprite">The AnimatedSprite to use when drawing the slime.</param>
    /// <param name="difficultyService">The difficulty service to handle input.</param>
    public Slime(AnimatedSprite sprite, SlimeDifficultyService difficultyService = null)
    {
        _sprite = sprite;
        _difficultyService = difficultyService ?? new SlimeDifficultyService();
    }

    /// <summary>
    /// Initializes the slime, can be used to reset it back to an initial state.
    /// </summary>
    /// <param name="startingPosition">The position the slime should start at.</param>
    /// <param name="stride">The total number of pixels to move the head segment during each movement cycle.</param>
    public void Initialize(Vector2 startingPosition, float stride)
    {
        // Check if we're in easy mode
        _isEasyMode = _difficultyService.CurrentDifficulty == DifficultyMode.Easy;

        if (_isEasyMode)
        {
            // For easy mode, just track a single position
            _easyModePosition = startingPosition;
            _segments = new List<SlimeSegment>();
            
            // Create a single segment for easy mode
            SlimeSegment head = new SlimeSegment();
            head.At = startingPosition;
            head.To = startingPosition;
            head.Direction = Vector2.UnitX;
            _segments.Add(head);
        }
        else
        {
            // Initialize the segment collection for hard mode.
            _segments = new List<SlimeSegment>();

            // Set the stride
            _stride = stride;

            // Create the initial head of the slime chain.
            SlimeSegment head = new SlimeSegment();
            head.At = startingPosition;
            head.To = startingPosition + new Vector2(_stride, 0);
            head.Direction = Vector2.UnitX;

            // Add it to the segment collection.
            _segments.Add(head);

            // Set the initial next direction as the same direction the head is
            // moving.
            _nextDirection = head.Direction;

            // initialize the input buffer.
            _inputBuffer = new Queue<Vector2>(MAX_BUFFER_SIZE);
        }

        // Zero out the movement timer.
        _movementTimer = TimeSpan.Zero;
    }

    private void HandleInput()
    {
        if (_isEasyMode)
        {
            // Easy mode: Get direction vector for continuous movement
            Vector2 direction = _difficultyService.HandleEasyInput();
            
            // Apply movement directly to position
            if (direction != Vector2.Zero)
            {
                float speed = _difficultyService.GetMovementSpeed();
                Vector2 oldPosition = _easyModePosition;
                _easyModePosition += direction * speed;
                
                // Update all segments to follow the head in easy mode
                UpdateEasyModeSegments(oldPosition);
            }
        }
        else
        {
            // Hard/Medium mode: Use input buffering for discrete movement
            _difficultyService.HandleHardInput(ref _inputBuffer, ref _segments);
        }
    }

    private void UpdateEasyModeSegments(Vector2 previousHeadPosition)
    {
        if (_segments.Count == 0) return;

        // Update the head segment position
        SlimeSegment head = _segments[0];
        head.At = _easyModePosition;
        head.To = _easyModePosition;
        _segments[0] = head;

        // For trailing segments in easy mode, create a simple following behavior
        // Each segment follows the position of the segment in front of it with a delay
        const float segmentSpacing = 48f; // Increased distance between segments for easy mode
        
        for (int i = 1; i < _segments.Count; i++)
        {
            SlimeSegment segment = _segments[i];
            SlimeSegment previousSegment = _segments[i - 1];
            
            // Calculate direction from current segment to previous segment
            Vector2 directionToPrevious = previousSegment.At - segment.At;
            
            // If the distance is greater than spacing, move towards the previous segment
            if (directionToPrevious.Length() > segmentSpacing)
            {
                directionToPrevious.Normalize();
                segment.At = previousSegment.At - directionToPrevious * segmentSpacing;
                segment.To = segment.At;
                _segments[i] = segment;
            }
        }
    }

    private void Move()
    {
        // Only perform discrete movement in hard mode
        if (!_isEasyMode)
        {
            // Get the next direction from the input buffer if one is available
            if (_inputBuffer.Count > 0)
            {
                _nextDirection = _inputBuffer.Dequeue();
            }

            // Capture the value of the head segment
            SlimeSegment head = _segments[0];

            // Update the direction the head is supposed to move in to the
            // next direction cached.
            head.Direction = _nextDirection;

            // Update the head's "at" position to be where it was moving "to"
            head.At = head.To;

            // Update the head's "to" position to the next tile in the direction
            // it is moving.
            head.To = head.At + head.Direction * _stride;

            // Insert the new adjusted value for the head at the front of the
            // segments and remove the tail segment. This effectively moves
            // the entire chain forward without needing to loop through every
            // segment and update its "at" and "to" positions.
            _segments.Insert(0, head);
            _segments.RemoveAt(_segments.Count - 1);

            // Iterate through all of the segments except the head and check
            // if they are at the same position as the head. If they are, then
            // the head is colliding with a body segment and a body collision
            // has occurred.
            for (int i = 1; i < _segments.Count; i++)
            {
                SlimeSegment segment = _segments[i];

                if (head.At == segment.At)
                {
                    if (BodyCollision != null)
                    {
                        BodyCollision.Invoke(this, EventArgs.Empty);
                    }

                    return;
                }
            }
        }
    }

    /// <summary>
    /// Informs the slime to grow by one segment.
    /// </summary>
    public void Grow()
    {
        if (_isEasyMode)
        {
            // In easy mode, we can grow by making the sprite larger or 
            // adding trailing segments that follow the main position
            SlimeSegment newSegment = new SlimeSegment();
            newSegment.At = _easyModePosition;
            newSegment.To = _easyModePosition;
            newSegment.Direction = Vector2.UnitX;
            _segments.Add(newSegment);
        }
        else
        {
            // Hard mode: Original growing logic
            // Capture the value of the tail segment
            SlimeSegment tail = _segments[_segments.Count - 1];

            // Create a new tail segment that is positioned a grid cell in the
            // reverse direction from the tail moving to the tail.
            SlimeSegment newTail = new SlimeSegment();
            newTail.At = tail.To + tail.ReverseDirection * _stride;
            newTail.To = tail.At;
            newTail.Direction = Vector2.Normalize(tail.At - newTail.At);

            // Add the new tail segment
            _segments.Add(newTail);
        }
    }

    /// <summary>
    /// Updates the slime.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        // Update the animated sprite.
        _sprite.Update(gameTime);

        // Handle any player input
        HandleInput();

        if (!_isEasyMode)
        {
            // Hard/Medium mode: Use timer-based discrete movement
            // Increment the movement timer by the frame elapsed time.
            _movementTimer += gameTime.ElapsedGameTime;

            // Get the appropriate movement time based on difficulty
            TimeSpan movementTime = _difficultyService.CurrentDifficulty == DifficultyMode.Medium 
                ? s_mediumMovementTime 
                : s_movementTime;

            // If the movement timer has accumulated enough time to be greater than
            // the movement time threshold, then perform a full movement.
            if (_movementTimer >= movementTime)
            {
                _movementTimer -= movementTime;
                Move();
            }

            // Update the movement lerp offset amount
            _movementProgress = (float)(_movementTimer.TotalSeconds / movementTime.TotalSeconds);
        }
        else
        {
            // Easy mode: Continuously update segment positions for smooth following
            if (_segments.Count > 1)
            {
                UpdateEasyModeSegments(_easyModePosition);
            }
            _movementProgress = 0f; // No interpolation needed in easy mode
        }
    }

    /// <summary>
    /// Draws the slime.
    /// </summary>
    public void Draw()
    {
        if (_isEasyMode)
        {
            // Easy mode: Draw all segments at their direct positions
            foreach (SlimeSegment segment in _segments)
            {
                _sprite.Draw(Core.SpriteBatch, segment.At);
            }
        }
        else
        {
            // Hard mode: Iterate through each segment and draw it
            foreach (SlimeSegment segment in _segments)
            {
                // Calculate the visual position of the segment at the moment by
                // lerping between its "at" and "to" position by the movement
                // offset lerp amount
                Vector2 pos = Vector2.Lerp(segment.At, segment.To, _movementProgress);

                // Draw the slime sprite at the calculated visual position of this
                // segment
                _sprite.Draw(Core.SpriteBatch, pos);
            }
        }
    }

    /// <summary>
    /// Returns a Circle value that represents collision bounds of the slime.
    /// </summary>
    /// <returns>A Circle value.</returns>
    public Circle GetBounds()
    {
        Vector2 pos;

        if (_isEasyMode)
        {
            // Easy mode: Use direct position
            pos = _easyModePosition;
        }
        else
        {
            // Hard mode: Use interpolated head position
            SlimeSegment head = _segments[0];

            // Calculate the visual position of the head at the moment of this
            // method call by lerping between the "at" and "to" position by the
            // movement offset lerp amount
            pos = Vector2.Lerp(head.At, head.To, _movementProgress);
        }

        // Create the bounds using the calculated visual position of the head.
        Circle bounds = new Circle(
            (int)(pos.X + (_sprite.Width * 0.5f)),
            (int)(pos.Y + (_sprite.Height * 0.5f)),
            (int)(_sprite.Width * 0.5f)
        );

        return bounds;
    }

    /// <summary>
    /// Sets the difficulty mode for the slime.
    /// </summary>
    /// <param name="mode">The difficulty mode to set.</param>
    public void SetDifficultyMode(DifficultyMode mode)
    {
        _difficultyService.CurrentDifficulty = mode;
        _isEasyMode = mode == DifficultyMode.Easy;
    }

}
