using DungeonSlime.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonSlime.Services
{
    public enum DifficultyMode
    {
        Easy,   // Direct position-based movement
        Medium, // Faster discrete movement with smaller buffer
        Hard    // Discrete tile-based movement with input buffering
    }

    public class SlimeDifficultyService
    {
        // The maximum size of the buffer queue for hard mode.
        private const int MAX_BUFFER_SIZE = 2;
        
        // The maximum size of the buffer queue for medium mode (smaller for more responsive controls).
        private const int MEDIUM_BUFFER_SIZE = 3;

        // Speed multiplier when moving.
        private const float EASY_MOVEMENT_SPEED = 7.0f;   // Faster for easy mode
        private const float NORMAL_MOVEMENT_SPEED = 5.0f;

        public DifficultyMode CurrentDifficulty { get; set; } = DifficultyMode.Easy;

        /// <summary>
        /// Handles input for easy mode - returns a direction vector for continuous movement
        /// </summary>
        /// <returns>A direction vector for movement this frame</returns>
        public Vector2 HandleEasyInput()
        {
            Vector2 direction = Vector2.Zero;

            // Get a reference to the keyboard info
            KeyboardInfo keyboard = Core.Input.Keyboard;

            // Check for direct movement inputs using IsKeyDown for continuous movement
            if (keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up))
            {
                direction.Y = -1;
            }
            else if (keyboard.IsKeyDown(Keys.S) || keyboard.IsKeyDown(Keys.Down))
            {
                direction.Y = 1;
            }

            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
            {
                direction.X = -1;
            }
            else if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
            {
                direction.X = 1;
            }

            // Normalize diagonal movement
            if (direction != Vector2.Zero)
            {
                direction.Normalize();
            }

            return direction;
        }

        public void HandleHardInput(ref Queue<Vector2> _inputBuffer, ref List<SlimeSegment> _segments)
        {
            Vector2 potentialNextDirection = Vector2.Zero;

            if (GameController.MoveUp())
            {
                potentialNextDirection = -Vector2.UnitY;
            }
            else if (GameController.MoveDown())
            {
                potentialNextDirection = Vector2.UnitY;
            }
            else if (GameController.MoveLeft())
            {
                potentialNextDirection = -Vector2.UnitX;
            }
            else if (GameController.MoveRight())
            {
                potentialNextDirection = Vector2.UnitX;
            }

            // If a new direction was input, consider adding it to the buffer
            if (potentialNextDirection != Vector2.Zero && _inputBuffer.Count < MAX_BUFFER_SIZE)
            {
                // If the buffer is empty, validate against the current direction;
                // otherwise, validate against the last buffered direction
                Vector2 validateAgainst = _inputBuffer.Count > 0 ?
                                          _inputBuffer.Last() :
                                          _segments[0].Direction;

                // Only allow direction change if it is not reversing the current
                // direction.  This prevents th slime from backing into itself
                float dot = Vector2.Dot(potentialNextDirection, validateAgainst);
                if (dot >= 0)
                {
                    _inputBuffer.Enqueue(potentialNextDirection);
                }
            }
        }

        public void HandleMediumInput(ref Queue<Vector2> _inputBuffer, ref List<SlimeSegment> _segments)
        {
            Vector2 potentialNextDirection = Vector2.Zero;

            if (GameController.MoveUp())
            {
                potentialNextDirection = -Vector2.UnitY;
            }
            else if (GameController.MoveDown())
            {
                potentialNextDirection = Vector2.UnitY;
            }
            else if (GameController.MoveLeft())
            {
                potentialNextDirection = -Vector2.UnitX;
            }
            else if (GameController.MoveRight())
            {
                potentialNextDirection = Vector2.UnitX;
            }

            // If a new direction was input, consider adding it to the buffer
            // Medium mode uses a smaller buffer for more responsive controls
            if (potentialNextDirection != Vector2.Zero && _inputBuffer.Count < MEDIUM_BUFFER_SIZE)
            {
                // If the buffer is empty, validate against the current direction;
                // otherwise, validate against the last buffered direction
                Vector2 validateAgainst = _inputBuffer.Count > 0 ?
                                          _inputBuffer.Last() :
                                          _segments[0].Direction;

                // Only allow direction change if it is not reversing the current
                // direction.  This prevents th slime from backing into itself
                float dot = Vector2.Dot(potentialNextDirection, validateAgainst);
                if (dot >= 0)
                {
                    _inputBuffer.Enqueue(potentialNextDirection);
                }
            }
        }

        /// <summary>
        /// Gets the movement speed for the current difficulty
        /// </summary>
        public float GetMovementSpeed()
        {
            return CurrentDifficulty == DifficultyMode.Easy ? EASY_MOVEMENT_SPEED : NORMAL_MOVEMENT_SPEED;
        }
    }
}
