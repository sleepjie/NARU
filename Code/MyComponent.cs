using Sandbox;
using SpriteTools;
using System;

public sealed class MyComponent : Component
{
	[RequireComponent] SpriteComponent Sprite { get; set; }
	[Property] float Speed { get; set; } = 200f;
	[Property] float MaxJumpHeight { get; set; } = 150f;
	[Property] float JumpForce { get; set; } = 300f;
	[Property] float Gravity { get; set; } = 800f;
	[Property] float FallMultiplier { get; set; } = 2.5f;
	[Property] float LowJumpMultiplier { get; set; } = 2f;
	[Property] float GroundCheckDistance { get; set; } = 5f;

	SceneTrace trace { get; set; }

	Vector2 Velocity = Vector2.Zero;
	bool IsGrounded = true;
	bool WasGrounded = true;
	float InitialJumpY;
	bool IsJumping = false;
	protected override void OnFixedUpdate()
	{
		ApplyGravity();
		CheckGroundStatus();
		BuildWishVelocity();
		Move();
	}

	protected override void OnStart()
	{
		Sprite.OnBroadcastEvent += OnBroadcastEvent;
		Sprite.OnAnimationComplete += OnAnimationComplete;
	}

	void BuildWishVelocity()
	{
		var input = Input.AnalogMove;

		// Horizontal movement
		Velocity.x = -input.y * Speed; // A/D keys for left/right movement

		// Initiate jump with W key
		if ( input.x > 0 )
		{
			Log.Info( "Jumping!" );
			IsJumping = true;
			IsGrounded = false;
			InitialJumpY = WorldPosition.y;
			Velocity.y = JumpForce;
			Sprite.PlayAnimation( "Jump" );
		}

		// Sustain jump while W is held and max height not reached
		if ( IsJumping && input.y > 0 && WorldPosition.y > InitialJumpY - MaxJumpHeight )
		{
			Velocity.y = -JumpForce * Time.Delta;
		}
		else
		{
			IsJumping = false;
		}

		// Increase fall speed with S key
		if ( -input.y > 0 && !IsGrounded )
		{
			Velocity.y -= Gravity * (FallMultiplier - 1) * Time.Delta;
		}
	}

	void ApplyGravity()
	{
		if ( !IsGrounded )
			Velocity.y -= Gravity * Time.Delta;
	}

	void CheckGroundStatus()
	{
		float GroundCheckDistance = 5.0f;

		var start = WorldPosition + new Vector3(Vector2.Up) * 0.1f;
		var end = start + new Vector3( Vector2.Down ) * GroundCheckDistance;

		var trace = Scene.Trace.Ray( start, end ).WithTag( "Ground" ).Run();
		Log.Info( trace.Hit );
		IsGrounded = trace.Hit;

		if ( IsGrounded )
		{
			WorldPosition = trace.HitPosition;
			Velocity.y = 0;
		}
	}

	void Move()
	{
		var newPosition = WorldPosition + new Vector3( Velocity.x, 0, 0 ) * Time.Delta;
		newPosition.y += Velocity.y * Time.Delta;

		WorldPosition = newPosition;
	}

	protected override void OnUpdate()
	{
		UpdateAnimations();
	}

	void OnBroadcastEvent( string name )
	{
		if ( name == "footstep" && IsGrounded )
		{
			Sound.Play( "ui.button.press", WorldPosition );
		}
	}

	void UpdateAnimations()
	{
		if ( !WasGrounded && IsGrounded )
		{
			Sprite.PlayAnimation( "Land" );
		}
		else if ( Velocity.x != 0 && IsGrounded )
		{
			Sprite.PlayAnimation( "Run" );
			Sprite.PlaybackSpeed = Velocity.x / Speed;
			Sprite.SpriteFlags = Velocity.x < 0 ? SpriteFlags.HorizontalFlip : SpriteFlags.None;
		}
		else if ( IsGrounded && Velocity.y > 0 )
		{
			Sprite.PlayAnimation( "Jump" );
		}
		else if ( !IsGrounded && Velocity.y < 0 )
		{
			Sprite.PlayAnimation( "Fall" );
		}
		else if ( IsGrounded )
		{
			Sprite.PlayAnimation( "Idle" );
		}

		WasGrounded = IsGrounded;
	}

	void OnAnimationComplete( string name )
	{
		if ( name == "Land" )
		{
			Sprite.PlayAnimation( "Idle" );
		}
	}
}
