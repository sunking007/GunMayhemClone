using Godot;
using System;

namespace GunMayhemClone;

public partial class PlayerUiCard : Control
{
	private Label _playerNameLabel;
	private Label _livesCountLabel;
	private TextureProgressBar _healthBar; // Matches our new texture progress bar type
	private TextureRect _characterIcon;

	public override void _Ready()
	{
		_playerNameLabel = GetNodeOrNull<Label>("LayoutContainer/MainDataLayout/HeaderRow/PlayerNameLabel");
		_livesCountLabel = GetNodeOrNull<Label>("LayoutContainer/MainDataLayout/HeaderRow/LivesCountLabel");
		_healthBar = GetNodeOrNull<TextureProgressBar>("HealthBar"); // Directly under root
		_characterIcon = GetNodeOrNull<TextureRect>("LayoutContainer/PortraitContainer/CharacterIcon");
	}

	public void SetupCard(string nameText, Texture2D iconTexture, float maxHp, int initialLives)
	{
		if (_playerNameLabel != null) _playerNameLabel.Text = nameText;
		if (_characterIcon != null) _characterIcon.Texture = iconTexture;
		
		if (_healthBar != null)
		{
			_healthBar.MaxValue = maxHp;
			_healthBar.Value = maxHp;
		}
		
		UpdateLivesDisplay(initialLives);
	}

	public void UpdateHealthDisplay(float currentHp)
	{
		if (_healthBar != null)
		{
			_healthBar.Value = currentHp;
		}
	}

	public void UpdateLivesDisplay(int remainingLives)
	{
		if (_livesCountLabel != null)
		{
			_livesCountLabel.Text = remainingLives.ToString();
		}
	}
}
