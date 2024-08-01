﻿using System.Text.Json.Serialization;

namespace B4mServer.Models;

public partial class User
{
	public int Id { get; set; }

	public string Name { get; set; } = null!;

	public string Password { get; set; } = null!;

	public decimal Joined { get; set; }

	public string Color { get; set; } = null!;

	// Navigational Properties
	[JsonIgnore]
	public virtual ICollection<Message> Messages { get; set; } = [];

	[JsonIgnore]
	public virtual ICollection<Channel> OwnedChannels { get; set; } = [];
}