# 🧧 TẾT VỀ – ĐƯỜNG VỀ NHÀ

### Tet is Home

> Một hành trình trở về quê nhà, nơi người chơi không chỉ chuẩn bị cho một cái Tết, mà còn tìm lại ý nghĩa của hai chữ "quê hương".

---

## 📌 1. GIỚI THIỆU

**Tết Về – Đường Về Nhà** (*Tet is Home*) là một trò chơi 3D thuộc thể loại:

- 🎮 Action-Adventure
- 🗺️ Exploration
- 📜 Quest
- ⚔️ Light Combat

Trò chơi lấy cảm hứng từ **văn hóa Tết Việt Nam, gia đình, làng quê và những giá trị truyền thống đang dần bị lãng quên**.

Người chơi vào vai một người trẻ trở về quê trong những ngày cuối năm. Trong thời gian ba ngày trước giao thừa, người chơi sẽ cùng người dân chuẩn bị cho một cái Tết đặc biệt, thực hiện các nhiệm vụ, khám phá làng quê, tương tác với NPC và đối mặt với những sự cố xảy ra trong làng.

Thay vì tập trung vào việc tiêu diệt kẻ thù, trò chơi tập trung vào:

- Khám phá thế giới
- Tương tác với NPC
- Thực hiện nhiệm vụ
- Các hoạt động Tết
- Combat nhẹ
- Bảo vệ cộng đồng
- Lựa chọn hội thoại
- Hệ thống **Niềm Tin Của Làng**
- Nhiều kết thúc dựa trên hành động của người chơi

> **Mục tiêu cuối cùng không phải là đánh bại một kẻ thù.**
>
> **Mục tiêu là khiến con người muốn trở về.**

---

## 👥 Thành viên thực hiện

Dự án được thực hiện bởi nhóm **3 thành viên** thuộc **Khoa Công Nghệ Thông Tin – Trường Đại học Đà Lạt**.

| STT | Họ và tên | MSSV | Lớp | Vai trò chính |
|---:|---|---:|---|---|
| 1 | **Lê Anh Khoa** | **2312647** | CTK47A | Player / Combat / Gameplay |
| 2 | **Trần Thị Phương Trang** | **2314288** | CTK47A | Enemy / AI / Environment |
| 3 | **Đỗ Đặng Diệu Linh** | **2312663** | CTK47A | Quest / NPC / UI / Dialogue |

### 👨‍💻 Lê Anh Khoa — Player / Combat / Gameplay

Phụ trách phát triển các hệ thống liên quan đến nhân vật người chơi và gameplay chính, bao gồm **Player Controller, Movement, Camera, Sprint, Jump, Dodge, Health, Stamina, Combat, Weapon, Interaction** và các cơ chế gameplay liên quan.

Ngoài ra, phụ trách xây dựng và quản lý các Scene:

- `Opening`
- `House`
- `Kitchen`
- `Temple`
- `FinalDefense`
- `Ending`

---

### 👨‍💻 Trần Thị Phương Trang — Enemy / AI / Environment

Phụ trách phát triển hệ thống Enemy và AI, bao gồm **Patrol, Detection, Chase, Attack, Damage, Hit, Retreat, Death, Enemy State** và các hành vi của từng loại Enemy.

Đồng thời phụ trách các hệ thống môi trường và sự kiện nguy hiểm như **Fire Event, Environmental Hazard** và Mini-Boss.

Phụ trách xây dựng và quản lý các Scene:

- `Garden`
- `RiceField`
- `Forest`
- `FireEvent`

---

### 👩‍💻 Đỗ Đặng Diệu Linh — Quest / NPC / UI / Dialogue

Phụ trách các hệ thống nội dung và tương tác với thế giới game, bao gồm **Quest Manager, Quest Objective, Quest Tracker, NPC Interaction, Dialogue, Dialogue Choice, Inventory, UI, Day/Night** và hệ thống **Village Trust**.

Phụ trách xây dựng và quản lý các Scene:

- `MainMenu`
- `Market`
- `Calligraphy`
- `ChildrenArea`
- `LanternVillage`

---

## 🤝 Nguyên tắc làm việc nhóm

Mỗi thành viên chịu trách nhiệm chính đối với các hệ thống và Scene được phân công. Tuy nhiên, các thành viên vẫn phối hợp và hỗ trợ lẫn nhau trong quá trình phát triển, tích hợp, kiểm thử và hoàn thiện sản phẩm.

Đối với **Scene/Map**, mỗi Scene chỉ có **một Scene Owner** chịu trách nhiệm chỉnh sửa trực tiếp nhằm hạn chế xung đột khi sử dụng Git.

Các thành viên có thể phát triển **Prefab, Script, Model, Animation, Audio và các tài nguyên riêng biệt** để thành viên phụ trách Scene tích hợp vào Map của mình.

---

## 🎮 2. THÔNG TIN DỰ ÁN

| Thuộc tính | Thông tin |
|---|---|
| **Tên game** | Tết Về – Đường Về Nhà |
| **Tên tiếng Anh** | Tet is Home |
| **Thể loại** | 3D Action-Adventure / Exploration / Quest / Light Combat |
| **Chế độ** | Single-player |
| **Có thể mở rộng** | Co-op 2–3 người |
| **Engine** | Unity |
| **Render Pipeline** | Universal Render Pipeline (URP) |
| **Ngôn ngữ lập trình** | C# |
| **Version Control** | Git + GitHub |
| **Thời lượng Demo** | 30–60 phút |
| **Số thành viên** | 3 |

---

## 🌸 3. Ý TƯỞNG CỐT LÕI

Game kể câu chuyện về một người trẻ trở về quê vào những ngày cuối năm.

Khi trở về, nhân vật nhận ra rằng những hoạt động Tết quen thuộc đang dần biến mất.

Chợ Tết vắng người.

Ông đồ không còn ai xin chữ.

Người trẻ lần lượt rời quê.

Cây mai cổ thụ giữa làng có nguy cơ bị chặt bỏ.

Nhân vật có ba ngày để cùng người dân chuẩn bị một cái Tết thật trọn vẹn.

Mục tiêu cuối cùng không phải là đánh bại một kẻ thù.

Mục tiêu là khiến con người muốn trở về.

---

## 🎯 4. MỤC TIÊU DỰ ÁN

- Xây dựng một game 3D hoàn chỉnh ở quy mô đồ án.
- Thể hiện văn hóa và không khí Tết Việt Nam.
- Kết hợp Exploration, Quest, NPC và Light Combat.
- Xây dựng hệ thống AI cho Enemy.
- Xây dựng hệ thống Quest và Dialogue.
- Xây dựng Inventory.
- Xây dựng Day/Night.
- Xây dựng hệ thống Village Trust.
- Xây dựng nhiều Ending.
- Áp dụng Git/GitHub trong phát triển nhóm.

---

## 🗺️ 5. CẤU TRÚC MAP

| Scene | Nội dung | Scene Owner |
|---|---|---|
| MainMenu | Menu chính | Diệu Linh |
| Opening | Con đường về quê | Anh Khoa |
| House | Nhà bà | Anh Khoa |
| Market | Chợ Tết | Diệu Linh |
| Garden | Vườn | Phương Trang |
| RiceField | Ruộng | Phương Trang |
| Forest | Rừng | Phương Trang |
| Calligraphy | Nhà văn hóa / Ông đồ | Diệu Linh |
| Kitchen | Khu làm bánh | Anh Khoa |
| ChildrenArea | Khu trẻ em | Diệu Linh |
| LanternVillage | Đường đèn lồng | Diệu Linh |
| FireEvent | Khu xảy ra cháy | Phương Trang |
| Temple | Sân đình / Cây mai | Anh Khoa |
| FinalDefense | Trận chiến cuối | Anh Khoa |
| Ending | Giao thừa | Anh Khoa |

---

## 🧩 6. CÁC HỆ THỐNG CHÍNH

### Player

- Movement
- Camera
- Sprint
- Jump
- Dodge
- Health
- Stamina
- Interaction

### Combat

- Light Attack
- Heavy Attack
- Block
- Dodge
- Damage
- Hit Detection
- Weapon System

### Enemy

- Patrol
- Detection
- Chase
- Attack
- Hit
- Retreat
- Death

Enemy gồm:
- Chó hoang
- Chuột
- Ong
- Thanh niên phá phách
- Mini-Boss

### Quest

- Quest Manager
- Objective
- Quest Start
- Quest Complete
- Quest Fail
- Reward
- Quest Tracker

### NPC

- NPC Interaction
- Dialogue
- Dialogue Choice
- NPC State
- NPC Event

### Inventory

- Item
- Item Slot
- Quest Item
- Consumable
- Add/Remove Item

### Day/Night

- 28 Tết
- 29 Tết
- 30 Tết
- Đêm giao thừa

### Village Trust

Village Trust bắt đầu ở: **50/100**

Các hành động của người chơi có thể làm chỉ số này tăng hoặc giảm.

- **80–100**: Tết Trọn Vẹn
- **50–79**: Một Mùa Tết Bình Yên
- **0–49**: Tết Vắng

---

## 📂 7. CẤU TRÚC PROJECT

```text
Assets/
│
├── _Project/                                      --- Toàn bộ tài nguyên chính do nhóm tự phát triển
│   │
│   ├── Art/                                       --- Model 3D, hình ảnh và tài nguyên đồ họa của game
│   │   ├── Characters/                            --- Model 3D của Player, NPC và Enemy
│   │   ├── Environment/                           --- Model 3D dùng xây dựng môi trường, làng quê, nhà cửa
│   │   ├── Props/                                 --- Các vật thể trang trí và vật thể tương tác trong game
│   │   ├── Vegetation/                            --- Cây cối, hoa, cỏ và thực vật
│   │   └── UI/                                    --- Hình ảnh, icon và tài nguyên đồ họa cho giao diện
│   │
│   ├── Audio/                                     --- Âm thanh của game
│   │   ├── Music/                                 --- Nhạc nền, nhạc Tết, nhạc combat và nhạc Ending
│   │   ├── SFX/                                   --- Hiệu ứng âm thanh: bước chân, đánh nhau, môi trường, vật thể
│   │   └── Voice/                                 --- Giọng nói, lời thoại và voice của nhân vật
│   │
│   ├── Animations/                                --- Animation và Animator Controller của các nhân vật
│   │   ├── Player/                                --- Animation của nhân vật người chơi
│   │   ├── Enemy/                                 --- Animation của các loại Enemy
│   │   └── NPC/                                   --- Animation của NPC và dân làng
│   │
│   ├── Materials/                                 --- Material và Texture dùng cho model, môi trường và vật thể
│   │
│   ├── Prefabs/                                   --- Các GameObject đã được cấu hình sẵn để tái sử dụng
│   │   ├── Player/                                --- Prefab của Player
│   │   ├── Enemy/                                 --- Prefab của các Enemy
│   │   ├── NPC/                                   --- Prefab của NPC và dân làng
│   │   ├── Environment/                           --- Prefab của nhà cửa, cây cối, đồ trang trí và môi trường
│   │   ├── Items/                                 --- Prefab của các vật phẩm trong game
│   │   ├── Weapons/                               --- Prefab của các vũ khí và vật dụng dùng trong Combat
│   │   └── UI/                                    --- Prefab của các thành phần giao diện
│   │
│   ├── Scenes/                                    --- Toàn bộ Scene/Map của game
│   │   ├── 00_MainMenu/                           --- Scene menu chính
│   │   ├── 01_Opening/                            --- Scene mở đầu và giới thiệu nhân vật
│   │   ├── 02_House/                              --- Scene nhà bà và khu vực nhà chính
│   │   ├── 03_Market/                             --- Scene chợ Tết
│   │   ├── 04_Garden/                             --- Scene khu vườn
│   │   ├── 05_RiceField/                          --- Scene ruộng
│   │   ├── 06_Forest/                             --- Scene khu rừng
│   │   ├── 07_Calligraphy/                        --- Scene nhà văn hóa và ông đồ
│   │   ├── 08_Kitchen/                            --- Scene khu vực gói và nấu bánh
│   │   ├── 09_ChildrenArea/                       --- Scene khu vực trẻ em
│   │   ├── 10_LanternVillage/                    --- Scene đường làng trang trí đèn lồng
│   │   ├── 11_FireEvent/                          --- Scene sự kiện cháy và cứu hộ
│   │   ├── 12_Temple/                             --- Scene sân đình và cây mai cổ thụ
│   │   ├── 13_FinalDefense/                       --- Scene trận chiến bảo vệ làng cuối game
│   │   └── 14_Ending/                             --- Scene giao thừa và các Ending
│   │
│   ├── Scripts/                                   --- Toàn bộ mã nguồn C# của game
│   │   ├── Core/                                  --- Các hệ thống nền tảng dùng chung cho toàn bộ game
│   │   ├── Player/                                --- Logic điều khiển và trạng thái của Player
│   │   ├── Combat/                                --- Hệ thống chiến đấu, Damage, Health và Weapon
│   │   ├── Enemy/                                 --- Logic Enemy và hệ thống AI
│   │   ├── NPC/                                   --- Logic tương tác và trạng thái của NPC
│   │   ├── Quest/                                 --- Hệ thống nhiệm vụ và quản lý Quest
│   │   ├── Inventory/                             --- Hệ thống túi đồ và quản lý Item
│   │   ├── Dialogue/                              --- Hệ thống hội thoại và lựa chọn của người chơi
│   │   ├── UI/                                    --- Logic điều khiển các giao diện trong game
│   │   ├── World/                                 --- Các hệ thống và sự kiện liên quan đến thế giới
│   │   ├── DayNight/                              --- Hệ thống thời gian và chu kỳ ngày/đêm
│   │   ├── VillageTrust/                          --- Hệ thống Niềm Tin Của Làng
│   │   └── SaveSystem/                            --- Hệ thống lưu và tải tiến trình game
│   │
│   ├── ScriptableObjects/                         --- Dữ liệu game được quản lý bằng ScriptableObject
│   │   ├── Items/                                 --- Dữ liệu của các vật phẩm
│   │   ├── Quests/                                --- Dữ liệu của các nhiệm vụ
│   │   ├── Dialogue/                              --- Dữ liệu hội thoại và lựa chọn
│   │   ├── Characters/                            --- Dữ liệu nhân vật, NPC và Enemy
│   │   └── Weapons/                               --- Dữ liệu vũ khí và thông số Combat
│   │
│   ├── UI/                                        --- Tài nguyên giao diện người chơi
│   │   ├── HUD/                                   --- Thanh HP, Stamina, Village Trust, Quest Tracker
│   │   ├── Menus/                                 --- Main Menu, Pause Menu, Settings và Game Over
│   │   ├── Dialogue/                              --- Giao diện hộp thoại và lựa chọn hội thoại
│   │   ├── Inventory/                             --- Giao diện túi đồ và vật phẩm
│   │   └── Quest/                                 --- Giao diện nhiệm vụ và mục tiêu
│   │
│   ├── VFX/                                       --- Hiệu ứng hình ảnh trong game
│   │   ├── Combat/                                --- Hiệu ứng đánh, trúng đòn, Damage
│   │   ├── Environment/                           --- Hiệu ứng môi trường, khói, lửa, bụi
│   │   └── Special/                               --- Các hiệu ứng đặc biệt như pháo hoa, đèn lồng
│   │
│   ├── Resources/                                 --- Tài nguyên cần được tải động trong Runtime
│   │
│   └── Settings/                                  --- Các thiết lập và cấu hình của Project
│       ├── Input/                                 --- Thiết lập Input và điều khiển
│       ├── Audio/                                 --- Thiết lập âm thanh
│       ├── Graphics/                              --- Thiết lập đồ họa
│       └── Game/                                  --- Thiết lập chung của game
│
└── ThirdParty/                                    --- Asset, Plugin và Package từ bên thứ ba
    ├── Assets/                                    --- Asset bên ngoài được sử dụng trong project
    ├── Plugins/                                   --- Plugin và thư viện bên thứ ba
    └── Tools/                                     --- Công cụ hỗ trợ phát triển game