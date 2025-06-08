<<<<<<< Updated upstream
# Avalonia 聊天客户端

## 部署问题
由于SukiChat、SIPSorceryMedia.Windows和Notification.Avalonia是通过本地dll引入项目的，需要自行添加.dll，另外，因为修改了一些源码，请使用项目提供的dll，我把dll放在了ChatClient.Desktop/DLL文件夹下面了。

## 视频链接
https://www.bilibili.com/video/BV1mE5jzLEPp

## 项目简介
这是一个基于 Avalonia 框架的跨平台聊天客户端，采用 Prism 实现 MVVM 架构。  
支持文字聊天、文件传输、实时音视频通话等功能，具有良好的扩展性和可维护性。

通过模块化设计和依赖注入，实现了各模块间的解耦，便于团队协作开发和后期维护。

## 界面展示
- 聊天页面
![聊天页面](/Assets/ChatView.png)
- 通讯录页面
![通讯录用户信息页面](/Assets/ContactView_UserDetail.png)
- 实时语音通话页面
![实时语音通话页面](/Assets/Call.png)  
- 群发弹窗
![群发弹窗](/Assets/CreateGroup.png)

## 项目结构
1. **ChatClient.Avalonia**  
   - 自定义控件、主题样式  
   - Behavior、Converter 等 UI 支持  
2. **ChatClient.BaseService**  
   - Helper：平台辅助、消息/文件处理  
   - Manager：全局单例数据管理  
   - Mapper：Protobuf↔实体↔DTO 映射  
   - MessageHandler：接收服务器推送  
   - Services：消息发送与数据库存取  
3. **ChatServer.Client**  
   - Socket 客户端  
   - MessageOperate/Processor：Protobuf 消息处理  
   - EventAggregator 消息分发  
4. **ChatClient.DataBase**  
   - SQLite 本地数据库  
   - 聊天记录、联系人等持久化  
5. **ChatClient.Desktop**  
   - 程序入口  
   - Prism MVVM 框架实现  
6. **ChatClient.Media**  
   - WebRTC 实时音视频通话  
7. **ChatClient.Resources**  
   - HTTP 短连接文件上传/下载  
8. **ChatClient.Tool**  
   - DTO 定义：数据传输对象，用于不同层级间数据交换
   - Prism EventAggregator 事件：定义所有事件类型，实现模块间的解耦通信
   - BaseService 接口声明：定义服务接口，支持依赖注入和单元测试
   - 工具类和扩展方法：提高代码复用性
9. **ChatClient.Common**  
   - Protobuf 消息格式定义：客户端与服务端的统一协议
   - 常量定义和枚举类型
   - 共享的基础类和工具

## 代码功能实现
### a. 通信实现
- 基于 Socket 和 Protobuf 实现消息编解码  
- 使用 Prism EventAggregator 在模块间发布/订阅消息
- 心跳检测机制确保连接稳定性
- 重连机制处理网络波动
- 消息队列确保消息有序传递
- 消息状态追踪（发送中、已发送、已读）

### b. 数据存储
- SQLite 数据库初始化与迁移  
- EF Core 或直接 SQL 操作本地持久化用户、聊天记录等
- 数据库表设计：用户表、好友关系表、群组表、消息记录表、文件记录表
- 数据加密保证用户隐私
- 查询优化确保大量消息记录下的流畅体验
- 定期数据清理和备份机制

### c. 文件处理
- HTTP 短连接实现文件上传/下载  
- 本地临时缓存与断点续传支持  
- 文件分片上传与断点续传
- 上传/下载进度实时显示
- 文件预览功能（图片、文档等）
- 自动文件类型识别与处理

### d. 媒体处理
- WebRTC 点对点视频通话  
- 音频编码/解码与回声消除  
- 视频流渲染与多路同步
- 自适应网络带宽控制
- 支持屏幕共享功能
- 多人会议模式支持
=======
# Avalonia聊天客户端

## 项目简介

这是一个基于Avalonia框架的客户端程序，是一个功能完善的聊天软件。项目采用Prism框架实现MVVM架构模式，具有良好的可扩展性和可维护性。

## 项目架构

项目采用模块化设计，各模块职责明确，通过接口进行解耦，使用Prism框架的EventAggregator实现模块间通信。

## 项目组成

### 1. ChatClient.Avalonia

Avalonia的UI组件库，包含：
- 自定义控件
- 主题样式定义
- Behavior实现
- 值转换器(Converter)

### 2. ChatClient.BaseService

项目的核心业务逻辑层，包含：
- **Helper**: 平台特定的辅助类、消息发送和文件处理辅助工具
- **Manager**: 单例类，负责管理全局数据
- **Mapper**: 实现Protobuf、数据库实体、Dto之间的数据转换
- **MessageHandler**: 用于登录后接收服务器主动发送的消息和通知
- **Services**: 处理消息发送，数据库存取等核心业务

### 3. ChatServer.Client

程序的Socket客户端模块，负责与服务器通信：
- **MessageOperate/Processor**: 用于接收和处理Protobuf格式的Message消息
- 通过EventAggregator将接收到的消息通知给其他模块

### 4. ChatClient.DataBase

客户端数据持久化模块：
- 使用SQLite本地数据库
- 负责保存用户聊天记录、联系人等数据

### 5. ChatClient.Desktop

程序入口项目：
- 基于Prism框架
- 实现MVVM架构模式
- 负责UI显示和用户交互

### 6. ChatClient.Media

音视频通话模块：
- 基于WebRTC技术
- 实现实时音视频通话功能

### 7. ChatClient.Resources

资源处理模块：
- 负责与资源服务器通信
- 采用HTTP短连接
- 实现用户文件的上传和下载

### 8. ChatClient.Tool

工具类库，包含：
- DTO数据传输对象定义
- Event事件定义(配合Prism框架的EventAggregator)
- BaseService项目中实现类的接口定义

### 9. ChatClient.Common

通用模块：
- 定义了Protobuf消息格式
- 服务端和客户端共用的基础类型

## 功能介绍

[此部分由用户自行完善]

## 界面展示

[此部分由用户自行完善]
>>>>>>> Stashed changes
