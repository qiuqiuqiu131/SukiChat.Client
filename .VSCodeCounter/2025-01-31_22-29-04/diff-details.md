# Diff Details

Date : 2025-01-31 22:29:04

Directory d:\\LanguageLearnig\\Avalonia\\ChatClient

Total : 40 files,  -3842 codes, 81 comments, 158 blanks, all -3603 lines

[Summary](results.md) / [Details](details.md) / [Diff Summary](diff.md) / Diff Details

## Files
| filename | language | code | comment | blank | total |
| :--- | :--- | ---: | ---: | ---: | ---: |
| [ChatClient.Avalonia/bin/Release/net8.0/ChatClient.Avalonia.deps.json](/ChatClient.Avalonia/bin/Release/net8.0/ChatClient.Avalonia.deps.json) | JSON | -1,103 | 0 | 0 | -1,103 |
| [ChatClient.BaseService/BaseServiceExtension.cs](/ChatClient.BaseService/BaseServiceExtension.cs) | C# | 3 | 1 | 1 | 5 |
| [ChatClient.BaseService/Manager/UserManager.cs](/ChatClient.BaseService/Manager/UserManager.cs) | C# | -81 | -7 | -9 | -97 |
| [ChatClient.BaseService/Mapper/DataToDtoProfile.cs](/ChatClient.BaseService/Mapper/DataToDtoProfile.cs) | C# | 25 | 1 | 5 | 31 |
| [ChatClient.BaseService/Mapper/ProtoToDtoProfile.cs](/ChatClient.BaseService/Mapper/ProtoToDtoProfile.cs) | C# | 22 | 0 | 3 | 25 |
| [ChatClient.BaseService/MessageHandler/FriendMessageHandler.cs](/ChatClient.BaseService/MessageHandler/FriendMessageHandler.cs) | C# | 95 | 10 | 11 | 116 |
| [ChatClient.BaseService/MessageHandler/IMessageHandler.cs](/ChatClient.BaseService/MessageHandler/IMessageHandler.cs) | C# | 6 | 0 | 1 | 7 |
| [ChatClient.BaseService/MessageHandler/MessageHandlerBase.cs](/ChatClient.BaseService/MessageHandler/MessageHandlerBase.cs) | C# | 25 | 0 | 8 | 33 |
| [ChatClient.BaseService/Services/FriendService.cs](/ChatClient.BaseService/Services/FriendService.cs) | C# | 37 | 10 | 6 | 53 |
| [ChatClient.BaseService/Services/UserLoginService.cs](/ChatClient.BaseService/Services/UserLoginService.cs) | C# | 114 | 33 | 28 | 175 |
| [ChatClient.BaseService/Services/UserService.cs](/ChatClient.BaseService/Services/UserService.cs) | C# | 4 | 10 | 3 | 17 |
| [ChatClient.Client/Client/SocketClient.cs](/ChatClient.Client/Client/SocketClient.cs) | C# | 2 | 1 | 1 | 4 |
| [ChatClient.Client/MessageOperate/Processor/FriendRequestFromUserResponseProcessor.cs](/ChatClient.Client/MessageOperate/Processor/FriendRequestFromUserResponseProcessor.cs) | C# | 4 | 0 | 2 | 6 |
| [ChatClient.Client/MessageOperate/Processor/OutlineMessageResponseProcessor.cs](/ChatClient.Client/MessageOperate/Processor/OutlineMessageResponseProcessor.cs) | C# | 4 | 0 | 2 | 6 |
| [ChatClient.Client/bin/Release/net8.0/ChatClient.Client.deps.json](/ChatClient.Client/bin/Release/net8.0/ChatClient.Client.deps.json) | JSON | -989 | 0 | 0 | -989 |
| [ChatClient.DataBase/ChatClientDbContext.cs](/ChatClient.DataBase/ChatClientDbContext.cs) | C# | 1 | 0 | 0 | 1 |
| [ChatClient.DataBase/DataBaseExtensions.cs](/ChatClient.DataBase/DataBaseExtensions.cs) | C# | -1 | 0 | 0 | -1 |
| [ChatClient.DataBase/DataToDtoProfile.cs](/ChatClient.DataBase/DataToDtoProfile.cs) | C# | -16 | 0 | -3 | -19 |
| [ChatClient.DataBase/Data/FriendReceived.cs](/ChatClient.DataBase/Data/FriendReceived.cs) | C# | -2 | 0 | 4 | 2 |
| [ChatClient.DataBase/Data/FriendRelation.cs](/ChatClient.DataBase/Data/FriendRelation.cs) | C# | -2 | 0 | 1 | -1 |
| [ChatClient.DataBase/Data/FriendRequest.cs](/ChatClient.DataBase/Data/FriendRequest.cs) | C# | -10 | 0 | 2 | -8 |
| [ChatClient.DataBase/bin/Release/net8.0/ChatClient.DataBase.deps.json](/ChatClient.DataBase/bin/Release/net8.0/ChatClient.DataBase.deps.json) | JSON | -1,599 | 0 | 0 | -1,599 |
| [ChatClient.DataBase/bin/Release/net8.0/ChatClient.DataBase.runtimeconfig.json](/ChatClient.DataBase/bin/Release/net8.0/ChatClient.DataBase.runtimeconfig.json) | JSON | -14 | 0 | 0 | -14 |
| [ChatClient.Desktop/App.axaml.cs](/ChatClient.Desktop/App.axaml.cs) | C# | 1 | 0 | 0 | 1 |
| [ChatClient.Desktop/ViewModels/ChatPages/ContactsViewModel.cs](/ChatClient.Desktop/ViewModels/ChatPages/ContactsViewModel.cs) | C# | 15 | 0 | 2 | 17 |
| [ChatClient.Desktop/ViewModels/Login/LoginViewModel.cs](/ChatClient.Desktop/ViewModels/Login/LoginViewModel.cs) | C# | 2 | 0 | 0 | 2 |
| [ChatClient.Desktop/ViewModels/Login/RegisterWindowViewModel.cs](/ChatClient.Desktop/ViewModels/Login/RegisterWindowViewModel.cs) | C# | 4 | 0 | 0 | 4 |
| [ChatClient.Desktop/Views/ChatPages/ContactsView.axaml](/ChatClient.Desktop/Views/ChatPages/ContactsView.axaml) | XML | 30 | 0 | 0 | 30 |
| [ChatClient.Tool/Data/FriendReceiveDto.cs](/ChatClient.Tool/Data/FriendReceiveDto.cs) | C# | 1 | 0 | 1 | 2 |
| [ChatClient.Tool/Data/FriendRelationDto.cs](/ChatClient.Tool/Data/FriendRelationDto.cs) | C# | 1 | 0 | 1 | 2 |
| [ChatClient.Tool/Data/OutlineMessageDto.cs](/ChatClient.Tool/Data/OutlineMessageDto.cs) | C# | 7 | 0 | 2 | 9 |
| [ChatClient.Tool/Data/UserData.cs](/ChatClient.Tool/Data/UserData.cs) | C# | 16 | 0 | 5 | 21 |
| [ChatClient.Tool/ProtoToDtoProfile.cs](/ChatClient.Tool/ProtoToDtoProfile.cs) | C# | -19 | 0 | -2 | -21 |
| [ChatClient.Tool/bin/Release/net8.0/ChatClient.Tool.deps.json](/ChatClient.Tool/bin/Release/net8.0/ChatClient.Tool.deps.json) | JSON | -921 | 0 | 0 | -921 |
| [ChatServer.Common/Protobuf/ChatRelationProtocol.cs](/ChatServer.Common/Protobuf/ChatRelationProtocol.cs) | C# | 261 | 6 | 22 | 289 |
| [ChatServer.Common/Protobuf/ChatUserProtocol.cs](/ChatServer.Common/Protobuf/ChatUserProtocol.cs) | C# | 590 | 16 | 61 | 667 |
| [ChatServer.Common/bin/Debug/net6.0/ChatServer.Common.deps.json](/ChatServer.Common/bin/Debug/net6.0/ChatServer.Common.deps.json) | JSON | -41 | 0 | 0 | -41 |
| [ChatServer.Common/bin/Debug/net6.0/Common.deps.json](/ChatServer.Common/bin/Debug/net6.0/Common.deps.json) | JSON | -41 | 0 | 0 | -41 |
| [ChatServer.Common/bin/Debug/net6.0/Protobuf\_Common.deps.json](/ChatServer.Common/bin/Debug/net6.0/Protobuf_Common.deps.json) | JSON | -41 | 0 | 0 | -41 |
| [ChatServer.Common/bin/Release/net8.0/ChatServer.Common.deps.json](/ChatServer.Common/bin/Release/net8.0/ChatServer.Common.deps.json) | JSON | -232 | 0 | 0 | -232 |

[Summary](results.md) / [Details](details.md) / [Diff Summary](diff.md) / Diff Details