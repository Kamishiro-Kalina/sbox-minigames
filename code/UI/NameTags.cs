using Sandbox.UI.Construct;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sandbox.UI
{
	public class BaseMiniGamesNameTag : Panel
	{
		public Label NameLabel;
		public Image Avatar;

		Player player;

		public BaseMiniGamesNameTag( Player player )
		{
			this.player = player;

			var client = player.Client;

			NameLabel = Add.Label( $"{client.Name}" );
			Avatar = Add.Image( $"avatar:{client.PlayerId}" );
		}

		public virtual void UpdateFromPlayer( Player player )
		{
			if (player is MiniGamesPlayer ply && ply.Team != null )
			{
				var color = ply.Team.Color.ToColor();

				Style.BackgroundColor = new Color( color.r, color.g, color.b, 0.85f );
				Style.Dirty();
			}
		}
	}

	public class MiniGamesNameTags : Panel
	{
		Dictionary<Player, BaseMiniGamesNameTag> ActiveTags = new();

		public float MaxDrawDistance = 400;
		public int MaxTagsToShow = 5;

		public MiniGamesNameTags()
		{
			StyleSheet.Load( "/UI/NameTags.scss" );
		}

		public override void Tick()
		{
			base.Tick();

			var deleteList = new List<Player>();
			deleteList.AddRange( ActiveTags.Keys );

			int count = 0;
			foreach ( var player in Entity.All.OfType<Player>().OrderBy( x => Vector3.DistanceBetween( x.Position, CurrentView.Position ) ) )
			{
				if ( UpdateNameTag( player ) )
				{
					deleteList.Remove( player );
					count++;
				}

				if ( count >= MaxTagsToShow )
					break;
			}

			foreach ( var player in deleteList )
			{
				ActiveTags[player].Delete();
				ActiveTags.Remove( player );
			}

		}

		public virtual BaseMiniGamesNameTag CreateNameTag( Player player )
		{
			if ( player.Client == null )
				return null;

			var tag = new BaseMiniGamesNameTag( player );
			tag.Parent = this;
			return tag;
		}

		public bool UpdateNameTag( Player player )
		{
			// Don't draw local player
			if ( player == Local.Pawn )
				return false;

			if ( player.LifeState != LifeState.Alive )
				return false;

			if ( player is MiniGamesPlayer ply && ply.Team is Spectator )
				return false;

			//
			// Where we putting the label, in world coords
			//
			var head = player.GetAttachment( "hat" ) ?? new Transform( player.EyePos );

			var labelPos = head.Position + head.Rotation.Up * 5;


			//
			// Are we too far away?
			//
			float dist = labelPos.Distance( CurrentView.Position );
			if ( dist > MaxDrawDistance )
				return false;

			//
			// Are we looking in this direction?
			//
			var lookDir = (labelPos - CurrentView.Position).Normal;
			if ( CurrentView.Rotation.Forward.Dot( lookDir ) < 0.5 )
				return false;

			// TODO - can we see them


			MaxDrawDistance = 400;

			// Max Draw Distance


			var alpha = dist.LerpInverse( MaxDrawDistance, MaxDrawDistance * 0.1f, true );

			// If I understood this I'd make it proper function
			var objectSize = 0.05f / dist / (2.0f * MathF.Tan( (CurrentView.FieldOfView / 2.0f).DegreeToRadian() )) * 1500.0f;

			objectSize = objectSize.Clamp( 0.05f, 1.0f );

			if ( !ActiveTags.TryGetValue( player, out var tag ) )
			{
				tag = CreateNameTag( player );
				if ( tag != null )
				{
					ActiveTags[player] = tag;
				}
			}

			if ( tag == null )
				return false;

			tag.UpdateFromPlayer( player );

			var screenPos = labelPos.ToScreen();

			tag.Style.Left = Length.Fraction( screenPos.x );
			tag.Style.Top = Length.Fraction( screenPos.y );
			tag.Style.Opacity = alpha;

			var transform = new PanelTransform();
			transform.AddTranslateY( Length.Fraction( -1.0f ) );
			transform.AddScale( objectSize );
			transform.AddTranslateX( Length.Fraction( -0.5f ) );

			tag.Style.Transform = transform;
			tag.Style.Dirty();

			return true;
		}
	}
}
