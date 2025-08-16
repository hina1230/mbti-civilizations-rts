# MBTI Civilizations RTS

A real-time strategy game featuring 16 unique civilizations based on MBTI personality types, built with Unity 2022.3 LTS.

## 🎮 Features

- **16 Unique MBTI Civilizations**: Each civilization has distinct traits, units, and playstyles based on their personality type
- **Multiplayer Support**: 1v1 to 4v4 matches using Unity Netcode for GameObjects
- **Resource Management**: Gather and manage Gold, Wood, Food, and Stone
- **Strategic Gameplay**: Inspired by StarCraft 2 and Age of Empires 3
- **Top-down RTS Camera**: Full camera control with zoom, rotation, and edge scrolling

## 🏛️ MBTI Civilizations

### Implemented
- **INTJ - The Strategist**: Masters of long-term planning and technological advancement
  - Unique Ability: Master Plan (30s boost to all production and research)
  - Strategic Planning System: Economic, Military, Technology, Defense focuses
  - Bonuses: +25% research speed, +15% planning efficiency

### Planned Civilizations
- **ENTJ - The Commander**: Military dominance and leadership
- **INTP - The Thinker**: Innovation through analysis and research
- **ENTP - The Debater**: Adaptable and resourceful in any situation
- **INFJ - The Advocate**: Defensive specialists with strong support abilities
- **ENFJ - The Protagonist**: Team synergy and morale bonuses
- **INFP - The Mediator**: Cultural and diplomatic advantages
- **ENFP - The Campaigner**: Exploration and expansion bonuses
- **ISTJ - The Logistician**: Economic efficiency and resource management
- **ESTJ - The Executive**: Production and organization bonuses
- **ISFJ - The Defender**: Defensive structures and healing
- **ESFJ - The Consul**: Population growth and happiness
- **ISTP - The Virtuoso**: Versatile units and quick adaptation
- **ESTP - The Entrepreneur**: Trade and raid bonuses
- **ISFP - The Adventurer**: Stealth and guerrilla tactics
- **ESFP - The Entertainer**: Morale and speed bonuses

## 🛠️ Technical Stack

- **Engine**: Unity 2022.3 LTS
- **Networking**: Unity Netcode for GameObjects
- **Services**: Unity Services (Relay, Lobby, Authentication)
- **UI**: TextMeshPro
- **Navigation**: Unity AI Navigation

## 📦 Installation

1. **Prerequisites**
   - Unity 2022.3 LTS
   - Git

2. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/mbti-civilizations-rts.git
   cd mbti-civilizations-rts
   ```

3. **Open in Unity**
   - Open Unity Hub
   - Click "Add" and select the project folder
   - Open with Unity 2022.3 LTS

4. **Install required packages**
   - Open Package Manager (Window > Package Manager)
   - Install:
     - Netcode for GameObjects
     - Unity Services (Core, Authentication, Relay, Lobby)
     - TextMeshPro
     - AI Navigation

## 🎯 Getting Started

1. **Set up Unity Services**
   - Window > General > Services
   - Link your project to Unity Cloud

2. **Create scenes**
   - Create a MainMenu scene
   - Create a Game scene
   - Add GameManager prefab to both scenes

3. **Configure Network**
   - Add NetworkManager component
   - Configure transport settings

## 🗂️ Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # Game management systems
│   ├── Civilizations/  # MBTI civilization implementations
│   ├── Units/          # Unit behaviors and systems
│   ├── Buildings/      # Building systems
│   ├── Resources/      # Resource management
│   ├── Networking/     # Multiplayer functionality
│   ├── UI/            # User interface
│   └── Camera/        # RTS camera controls
├── Prefabs/           # Reusable game objects
├── Materials/         # Visual materials
└── Resources/         # Runtime loaded assets
```

## 🎮 Controls

- **Camera Movement**: WASD or Arrow Keys
- **Camera Rotation**: Q/E or Right-click drag
- **Zoom**: Mouse wheel or Page Up/Down
- **Edge Scrolling**: Move mouse to screen edges
- **Unit Selection**: Left-click
- **Multiple Selection**: Shift + Left-click or drag box
- **Unit Commands**: Right-click
- **Control Groups**: Ctrl + 1-9 (set), 1-9 (select)

## 🚀 Development Roadmap

- [x] Core game architecture
- [x] INTJ civilization implementation
- [x] Basic unit system
- [x] Resource management
- [x] RTS camera system
- [x] Networking foundation
- [ ] Building system
- [ ] Combat system
- [ ] Remaining 15 MBTI civilizations
- [ ] Map editor
- [ ] AI opponents
- [ ] Ranked matchmaking
- [ ] Replay system

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the project
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Inspired by StarCraft 2 and Age of Empires 3
- MBTI personality framework by Myers-Briggs
- Unity Technologies for the game engine

## 📧 Contact

Project Link: [https://github.com/yourusername/mbti-civilizations-rts](https://github.com/yourusername/mbti-civilizations-rts)

---

**Note**: This project is currently in active development. Features and gameplay are subject to change.