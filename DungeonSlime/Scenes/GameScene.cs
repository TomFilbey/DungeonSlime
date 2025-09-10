using System;
using System.Collections.Generic;
using DungeonSlime.GameObjects;
using DungeonSlime.UI;
using DungeonSlime.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using MonoGameGum;
using MonoGameLibrary;
using MonoGameLibrary.Graphics;
using MonoGameLibrary.Scenes;

namespace DungeonSlime.Scenes;

public class GameScene : Scene
{
    private enum GameState
    {
        Playing,
        Paused,
        GameOver
    }

    // Reference to the slime.
    private Slime _slime;

    // Reference to the bat(s) - using list to support multiple bats
    private List<Bat> _bats;

    // Defines the tilemap to draw.
    private Tilemap _tilemap;

    // Defines the bounds of the room that the slime and bat are contained within.
    private Rectangle _roomBounds;

    // The sound effect to play when the slime eats a bat.
    private SoundEffect _collectSoundEffect;

    // Tracks the players score.
    private int _score;

    private GameSceneUI _ui;

    private GameState _state;

    // The grayscale shader effect.
    private Effect _grayscaleEffect;

    // The amount of saturation to provide the grayscale shader effect.
    private float _saturation = 1.0f;

    // The speed of the fade to grayscale effect.
    private const float FADE_SPEED = 0.02f;

    // Difficulty service for handling different control schemes
    private SlimeDifficultyService _difficultyService;

    public override void Initialize()
    {
        // LoadContent is called during base.Initialize().
        base.Initialize();

        // During the game scene, we want to disable exit on escape. Instead,
        // the escape key will be used to return back to the title screen.
        Core.ExitOnEscape = false;

        // Create the room bounds by getting the bounds of the screen then
        // using the Inflate method to "Deflate" the bounds by the width and
        // height of a tile so that the bounds only covers the inside room of
        // the dungeon tilemap.
        _roomBounds = Core.GraphicsDevice.PresentationParameters.Bounds;
        _roomBounds.Inflate(-_tilemap.TileWidth, -_tilemap.TileHeight);

        // Subscribe to the slime's BodyCollision event so that a game over
        // can be triggered when this event is raised.
        _slime.BodyCollision += OnSlimeBodyCollision;

        // Create any UI elements from the root element created in previous
        // scenes.
        GumService.Default.Root.Children.Clear();

        // Initialize the user interface for the game scene.
        InitializeUI();

        // Initialize a new game to be played.
        InitializeNewGame();
    }

    private void InitializeUI()
    {
        // Clear out any previous UI element incase we came here
        // from a different scene.
        GumService.Default.Root.Children.Clear();

        // Create the game scene ui instance.
        _ui = new GameSceneUI();

        // Subscribe to the events from the game scene ui.
        _ui.ResumeButtonClick += OnResumeButtonClicked;
        _ui.RetryButtonClick += OnRetryButtonClicked;
        _ui.QuitButtonClick += OnQuitButtonClicked;
    }

    private void OnResumeButtonClicked(object sender, EventArgs args)
    {
        // Change the game state back to playing.
        _state = GameState.Playing;
    }

    private void OnRetryButtonClicked(object sender, EventArgs args)
    {
        // Player has chosen to retry, so initialize a new game.
        InitializeNewGame();
    }

    private void OnQuitButtonClicked(object sender, EventArgs args)
    {
        // Player has chosen to quit, so return back to the title scene.
        Core.ChangeScene(new TitleScene());
    }

    private void InitializeNewGame()
    {
        // Calculate the position for the slime, which will be at the center
        // tile of the tile map.
        Vector2 slimePos = new Vector2();
        slimePos.X = (_tilemap.Columns / 2) * _tilemap.TileWidth;
        slimePos.Y = (_tilemap.Rows / 2) * _tilemap.TileHeight;

        // Initialize the slime.
        _slime.Initialize(slimePos, _tilemap.TileWidth);

        // Initialize the bat(s).
        foreach (var bat in _bats)
        {
            bat.RandomizeVelocity();
            PositionBatAwayFromSlime(bat);
        }

        // Reset the score.
        _score = 0;

        // Set the game state to playing.
        _state = GameState.Playing;
    }

    public override void LoadContent()
    {
        // Initialize the difficulty service
        _difficultyService = new SlimeDifficultyService();
        
        // Set the difficulty based on the title screen selection
        _difficultyService.CurrentDifficulty = TitleScene.SelectedDifficulty;

        // Create the texture atlas from the XML configuration file.
        TextureAtlas atlas = TextureAtlas.FromFile(Core.Content, "images/atlas-definition.xml");

        // Create the tilemap from the XML configuration file.
        _tilemap = Tilemap.FromFile(Content, "images/tilemap-definition.xml");
        _tilemap.Scale = new Vector2(4.0f, 4.0f);

        // Create the animated sprite for the slime from the atlas.
        AnimatedSprite slimeAnimation = atlas.CreateAnimatedSprite("slime-animation");
        slimeAnimation.Scale = new Vector2(4.0f, 4.0f);

        // Create the slime with the difficulty service.
        _slime = new Slime(slimeAnimation, _difficultyService);

        // Create the animated sprite for the bat from the atlas.
        AnimatedSprite batAnimation = atlas.CreateAnimatedSprite("bat-animation");
        batAnimation.Scale = new Vector2(4.0f, 4.0f);

        // Load the bounce sound effect for the bat.
        SoundEffect bounceSoundEffect = Content.Load<SoundEffect>("audio/bounce");

        // Create the bat(s) based on difficulty
        _bats = new List<Bat>();
        
        // Determine bat speed based on difficulty
        float batSpeedMultiplier = _difficultyService.CurrentDifficulty == DifficultyMode.Hard ? 2.0f : 1.0f;
        
        // Create first bat
        AnimatedSprite batAnimation1 = atlas.CreateAnimatedSprite("bat-animation");
        batAnimation1.Scale = new Vector2(4.0f, 4.0f);
        Bat bat1 = new Bat(batAnimation1, bounceSoundEffect);
        bat1.SetSpeedMultiplier(batSpeedMultiplier);
        _bats.Add(bat1);
        
        // Create second bat for easy mode
        if (_difficultyService.CurrentDifficulty == DifficultyMode.Easy)
        {
            AnimatedSprite batAnimation2 = atlas.CreateAnimatedSprite("bat-animation");
            batAnimation2.Scale = new Vector2(4.0f, 4.0f);
            Bat bat2 = new Bat(batAnimation2, bounceSoundEffect);
            bat2.SetSpeedMultiplier(batSpeedMultiplier);
            _bats.Add(bat2);
        }

        // Load the collect sound effect.
        _collectSoundEffect = Content.Load<SoundEffect>("audio/collect");

        // Load the grayscale effect.
        _grayscaleEffect = Content.Load<Effect>("effects/grayscaleEffect");
    }

    public override void Update(GameTime gameTime)
    {
        // Ensure the UI is always updated.
        _ui.Update(gameTime);

        if (_state != GameState.Playing)
        {
            // The game is in either a paused or game over state, so
            // gradually decrease the saturation to create the fading grayscale.
            _saturation = Math.Max(0.0f, _saturation - FADE_SPEED);

            // If its just a game over state, return back.
            if (_state == GameState.GameOver)
            {
                return;
            }
        }

        // If the pause button is pressed, toggle the pause state.
        if (GameController.Pause())
        {
            TogglePause();
        }

        // At this point, if the game is paused, just return back early.
        if (_state == GameState.Paused)
        {
            return;
        }

        // Update the slime.
        _slime.Update(gameTime);

        // Update the bats.
        foreach (var bat in _bats)
        {
            bat.Update(gameTime);
        }

        // Perform collision checks.
        CollisionChecks();
    }


    private void CollisionChecks()
    {
        // Capture the current bounds of the slime.
        Circle slimeBounds = _slime.GetBounds();

        // Check collision with each bat
        for (int i = 0; i < _bats.Count; i++)
        {
            Bat bat = _bats[i];
            Circle batBounds = bat.GetBounds();

            // First perform a collision check to see if the slime is colliding with
            // the bat, which means the slime eats the bat.
            if (slimeBounds.Intersects(batBounds))
            {
                // Check if it's a golden bat for bonus rewards
                bool wasGolden = bat.IsGolden;
                
                // Move the bat to a new position away from the slime.
                PositionBatAwayFromSlime(bat);

                // Randomize the velocity of the bat.
                bat.RandomizeVelocity();

                // Tell the slime to grow - twice if it was a golden bat
                if (wasGolden)
                {
                    _slime.Grow();
                    _slime.Grow();
                    // Golden bat gives double points too
                    _score += 200;
                }
                else
                {
                    _slime.Grow();
                    _score += 100;
                }

                // Update the score display on the UI.
                _ui.UpdateScoreText(_score);

                // Play the collect sound effect.
                Core.Audio.PlaySoundEffect(_collectSoundEffect);
            }

            // Finally, check if the bat is colliding with a wall by validating if
            // it is within the bounds of the room.  If it is outside the room
            // bounds, then it collided with a wall, and the bat should bounce
            // off of that wall.
            if (batBounds.Top < _roomBounds.Top)
            {
                bat.Bounce(Vector2.UnitY);
            }
            else if (batBounds.Bottom > _roomBounds.Bottom)
            {
                bat.Bounce(-Vector2.UnitY);
            }

            if (batBounds.Left < _roomBounds.Left)
            {
                bat.Bounce(Vector2.UnitX);
            }
            else if (batBounds.Right > _roomBounds.Right)
            {
                bat.Bounce(-Vector2.UnitX);
            }
        }

        // Next check if the slime is colliding with the wall by validating if
        // it is within the bounds of the room.  If it is outside the room
        // bounds, then it collided with a wall which triggers a game over.
        if (slimeBounds.Top < _roomBounds.Top ||
           slimeBounds.Bottom > _roomBounds.Bottom ||
           slimeBounds.Left < _roomBounds.Left ||
           slimeBounds.Right > _roomBounds.Right)
        {
            GameOver();
            return;
        }
    }

    private void PositionBatAwayFromSlime(Bat bat)
    {
        // Calculate the position that is in the center of the bounds
        // of the room.
        float roomCenterX = _roomBounds.X + _roomBounds.Width * 0.5f;
        float roomCenterY = _roomBounds.Y + _roomBounds.Height * 0.5f;
        Vector2 roomCenter = new Vector2(roomCenterX, roomCenterY);

        // Get the bounds of the slime and calculate the center position.
        Circle slimeBounds = _slime.GetBounds();
        Vector2 slimeCenter = new Vector2(slimeBounds.X, slimeBounds.Y);

        // Calculate the distance vector from the center of the room to the
        // center of the slime.
        Vector2 centerToSlime = slimeCenter - roomCenter;

        // Get the bounds of the bat.
        Circle batBounds = bat.GetBounds();

        // Calculate the amount of padding we will add to the new position of
        // the bat to ensure it is not sticking to walls
        int padding = batBounds.Radius * 2;

        // Calculate the new position of the bat by finding which component of
        // the center to slime vector (X or Y) is larger and in which direction.
        Vector2 newBatPosition = Vector2.Zero;
        if (Math.Abs(centerToSlime.X) > Math.Abs(centerToSlime.Y))
        {
            // The slime is closer to either the left or right wall, so the Y
            // position will be a random position between the top and bottom
            // walls.
            newBatPosition.Y = Random.Shared.Next(
                _roomBounds.Top + padding,
                _roomBounds.Bottom - padding
            );

            if (centerToSlime.X > 0)
            {
                // The slime is closer to the right side wall, so place the
                // bat on the left side wall.
                newBatPosition.X = _roomBounds.Left + padding;
            }
            else
            {
                // The slime is closer ot the left side wall, so place the
                // bat on the right side wall.
                newBatPosition.X = _roomBounds.Right - padding * 2;
            }
        }
        else
        {
            // The slime is closer to either the top or bottom wall, so the X
            // position will be a random position between the left and right
            // walls.
            newBatPosition.X = Random.Shared.Next(
                _roomBounds.Left + padding,
                _roomBounds.Right - padding
            );

            if (centerToSlime.Y > 0)
            {
                // The slime is closer to the top wall, so place the bat on the
                // bottom wall.
                newBatPosition.Y = _roomBounds.Top + padding;
            }
            else
            {
                // The slime is closer to the bottom wall, so place the bat on
                // the top wall.
                newBatPosition.Y = _roomBounds.Bottom - padding * 2;
            }
        }

        // Assign the new bat position.
        bat.Position = newBatPosition;
    }

    private void OnSlimeBodyCollision(object sender, EventArgs args)
    {
        GameOver();
    }

    private void TogglePause()
    {
        if (_state == GameState.Paused)
        {
            // We're now unpausing the game, so hide the pause panel.
            _ui.HidePausePanel();

            // And set the state back to playing.
            _state = GameState.Playing;
        }
        else
        {
            // We're now pausing the game, so show the pause panel.
            _ui.ShowPausePanel();

            // And set the state to paused.
            _state = GameState.Paused;

            // Set the grayscale effect saturation to 1.0f
            _saturation = 1.0f;
        }
    }

    private void GameOver()
    {
        // Save the high score
        bool isNewHighScore = HighScoreService.AddScore(_score, TitleScene.SelectedDifficulty);
        
        // Show the game over panel.
        _ui.ShowGameOverPanel();

        // Set the game state to game over.
        _state = GameState.GameOver;

        // Set the grayscale effect saturation to 1.0f
        _saturation = 1.0f;
        
        // TODO: Could show "NEW HIGH SCORE!" message if isNewHighScore is true
    }

    private void ToggleDifficulty()
    {
        // Toggle between easy and hard difficulty
        DifficultyMode newMode = _difficultyService.CurrentDifficulty == DifficultyMode.Easy 
            ? DifficultyMode.Hard 
            : DifficultyMode.Easy;
            
        _difficultyService.CurrentDifficulty = newMode;
        _slime.SetDifficultyMode(newMode);
        
        // Reinitialize the slime with the new difficulty
        Vector2 slimePos = new Vector2();
        slimePos.X = (_tilemap.Columns / 2) * _tilemap.TileWidth;
        slimePos.Y = (_tilemap.Rows / 2) * _tilemap.TileHeight;
        _slime.Initialize(slimePos, _tilemap.TileWidth);
    }

    public override void Draw(GameTime gameTime)
    {
        // Clear the back buffer.
        Core.GraphicsDevice.Clear(Color.CornflowerBlue);

        if (_state != GameState.Playing)
        {
            // We are in a game over state, so apply the saturation parameter.
            _grayscaleEffect.Parameters["Saturation"].SetValue(_saturation);

            // And begin the sprite batch using the grayscale effect.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp, effect: _grayscaleEffect);
        }
        else
        {
            // Otherwise, just begin the sprite batch as normal.
            Core.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
        }

        // Draw the tilemap
        _tilemap.Draw(Core.SpriteBatch);

        // Draw the slime.
        _slime.Draw();

        // Draw the bats.
        foreach (var bat in _bats)
        {
            bat.Draw();
        }

        // Always end the sprite batch when finished.
        Core.SpriteBatch.End();

        // Draw the UI.
        _ui.Draw();
    }


}
