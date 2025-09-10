using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;

namespace DungeonSlime.GameObjects;

public class Bat
{
    private const float MOVEMENT_SPEED = 5.0f;
    private const float GOLDEN_SPEED_MULTIPLIER = 2.0f;
    private const float GOLDEN_DURATION = 5.0f; // Golden state lasts 10 seconds
    private const float GOLDEN_CHANCE = 0.20f; // 20% chance to become golden

    // Speed multiplier for different difficulties
    private float _speedMultiplier = 1.0f;

    // Golden bat state
    private bool _isGolden = false;
    private float _goldenTimer = 0f;
     
    // The velocity of the bat that defines the direction and how much in that
    // direction to update the bats position each update cycle.
    private Vector2 _velocity;

    // The AnimatedSprite used when drawing the bat.
    private AnimatedSprite _sprite;

    // The sound effect to play when the bat bounces off the edge of the room.
    private SoundEffect _bounceSoundEffect;

    /// <summary>
    /// Gets or Sets the position of the bat.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets whether this bat is currently in golden state.
    /// </summary>
    public bool IsGolden => _isGolden;

    /// <summary>
    /// Creates a new Bat using the specified animated sprite and sound effect.
    /// </summary>
    /// <param name="sprite">The AnimatedSprite ot use when drawing the bat.</param>
    /// <param name="bounceSoundEffect">The sound effect to play when the bat bounces off a wall.</param>
    public Bat(AnimatedSprite sprite, SoundEffect bounceSoundEffect)
    {
        _sprite = sprite;
        _bounceSoundEffect = bounceSoundEffect;
    }

    /// <summary>
    /// Randomizes the velocity of the bat.
    /// </summary>
    public void RandomizeVelocity()
    {
        // Generate a random angle
        float angle = (float)(Random.Shared.NextDouble() * MathHelper.TwoPi);

        // Convert the angle to a direction vector
        float x = (float)Math.Cos(angle);
        float y = (float)Math.Sin(angle);
        Vector2 direction = new Vector2(x, y);

        // Check if this bat should become golden
        if (Random.Shared.NextSingle() < GOLDEN_CHANCE)
        {
            MakeGolden();
        }
        else
        {
            _isGolden = false;
            _goldenTimer = 0f;
            
            // Set sprite color to normal
            _sprite.Color = Color.White;
        }

        // Calculate final speed with all multipliers
        float finalSpeed = MOVEMENT_SPEED * _speedMultiplier;
        if (_isGolden)
        {
            finalSpeed *= GOLDEN_SPEED_MULTIPLIER;
        }

        // Multiply the direction vector by the movement speed to get the
        // final velocity
        _velocity = direction * finalSpeed;
    }

    /// <summary>
    /// Makes this bat golden for a limited time.
    /// </summary>
    private void MakeGolden()
    {
        _isGolden = true;
        _goldenTimer = GOLDEN_DURATION;
        
        // Set the sprite color to gold
        _sprite.Color = Color.Gold;
    }

    /// <summary>
    /// Sets the speed multiplier for this bat.
    /// </summary>
    /// <param name="multiplier">The speed multiplier to apply.</param>
    public void SetSpeedMultiplier(float multiplier)
    {
        _speedMultiplier = multiplier;
    }

    /// <summary>
    /// Handles a bounce event when the bat collides with a wall or boundary.
    /// </summary>
    /// <param name="normal">The normal vector of the surface the bat is bouncing against.</param>
    public void Bounce(Vector2 normal)
    {
        Vector2 newPosition = Position;

        // Adjust the position based on the normal to prevent sticking to walls.
        if (normal.X != 0)
        {
            // We are bouncing off a vertical wall (left/right).
            // Move slightly away from the wall in the direction of the normal.
            newPosition.X += normal.X * (_sprite.Width * 0.1f);
        }

        if (normal.Y != 0)
        {
            // We are bouncing off a horizontal wall (top/bottom).
            // Move slightly way from the wall in the direction of the normal.
            newPosition.Y += normal.Y * (_sprite.Height * 0.1f);
        }

        // Apply the new position
        Position = newPosition;

        // Normalize before reflecting
        normal.Normalize();

        // Apply reflection based on the normal.
        _velocity = Vector2.Reflect(_velocity, normal);

        // Play the bounce sound effect.
        Core.Audio.PlaySoundEffect(_bounceSoundEffect);
    }

    /// <summary>
    /// Returns a Circle value that represents collision bounds of the bat.
    /// </summary>
    /// <returns>A Circle value.</returns>
    public Circle GetBounds()
    {
        int x = (int)(Position.X + _sprite.Width * 0.5f);
        int y = (int)(Position.Y + _sprite.Height * 0.5f);
        int radius = (int)(_sprite.Width * 0.25f);

        return new Circle(x, y, radius);
    }

    /// <summary>
    /// Updates the bat.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        // Update the animated sprite
        _sprite.Update(gameTime);

        // Update golden timer if the bat is golden
        if (_isGolden)
        {
            _goldenTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            // Check if golden time has expired
            if (_goldenTimer <= 0f)
            {
                _isGolden = false;
                _goldenTimer = 0f;
                
                // Reset sprite color to normal
                _sprite.Color = Color.White;
                
                // Recalculate velocity with normal speed
                Vector2 direction = Vector2.Normalize(_velocity);
                float normalSpeed = MOVEMENT_SPEED * _speedMultiplier;
                _velocity = direction * normalSpeed;
            }
        }

        // Update the position of the bat based on the velocity.
        Position += _velocity;
    }

    /// <summary>
    /// Draws the bat.
    /// </summary>
    public void Draw()
    {
        _sprite.Draw(Core.SpriteBatch, Position);
    }

}
