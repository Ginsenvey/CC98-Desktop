 # CC98 Desktop

`CC98 桌面版` 是CC98跨平台计划的一部分。 

<a title="从 Microsoft 获取" href="https://apps.microsoft.com/detail/9NJ1LFJ8CDQ0?hl=zh-cn&gl=CN&ocid=pdpshare">  
    <img src="https://get.microsoft.com/images/zh-CN%20dark.svg" />
</a>

> CC98 Desktop是一个基于Windows App SDK开发的论坛客户端，采用FluentDesign绘制UI,旨在桌面端提供更加易于交互、触控友好、流畅快速的论坛浏览体验。  
>   
> 本应用适用于搭载Windows10/11、X64和ARM64架构的计算机。Android/IOS/Mac/Linux的跨平台版本正在早期开发阶段，将会使用新的UI风格。  
 
**此版本中的新功能**  
- 密码登录：现在支持Open-ID和账号密码登录。这样，你可以在校外无需RVPN访问论坛。  
  
- 自定义表情:你可以搜藏图片URL，从而像表情包一样使用它。  
  
- 新API驱动的抽卡，分解卡牌、查看抽卡统计信息。  
  
- 支持查看版面精华、编辑已发送的回复、查看“回复我的”、进行投票。取消已有收藏。  
  
- 支持关注、取关、私信其他用户。  
  
- 修改主面板的配色方案，在发帖时可选带上Windows平台小尾巴。  
  
- 支持除了“麻将脸”外的所有表情。  
  
- 一组UI更新和bug修复。  
  
- 更新无感知认证：无感知认证集成到更底层，更加快速稳定。  
  
- 更完整的内链跳转功能。  
  
  
**Issues**  
  
- UBB解析器不能应对复杂的嵌套情况。UBB呈现器不能渲染彩色文本/Font/ReplyView/等标签。Markdown呈现器不支持内联HTML/Latex。  
  
  
**下载&安装**  
  
  
在微软商店中安装正式版本。或者，在Github中追踪最新的预览版本。  
  
  
仓库：[https://github.com/Ginsenvey/CC98-Desktop ](https://github.com/Ginsenvey/CC98-Desktop )  
  
  
**更新**  
  
由微软商店提供自动更新。Github仓库会提供包含实验性功能的预览构建。  
  
  
**隐私和使用须知**  
  
应用使用Windows自带的凭据管理类`PasswordVault`存储令牌。应用不会存储CC98用户密码。应用会存储VPN账户和密码。  
  
应用只会与`CC98 API`，WebVPN,浙江大学镜像站和抽卡API交换信息，只缓存基础配置信息（可在实验性功能中查看。）  
  
**和网页端/小程序一样，不得以任何方式将CC98 Desktop的内容截图上传到外网。具体规则按照CC98官方所撰写的用户须知。**  
  
当您处于外网时，程序不会显示任何有效内容。请配合`ZJU Connect`，或者使用内置WebVPN在外网访问本应用。  
  
**Contribution**  
  
本客户端使用WinUI3框架/C#/XAML进行编写。  
  
欢迎加入CC98 Desktop的开发，并对代码/UI设计提供意见。  
  
你可以在开发者的Github Issue处，或[CC98 桌面客户端的开发进度记录楼](https://www.cc98.org/topic/6173309)反馈问题。  
  
你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。  

##### 设置开发环境
1. 确保您安装了Windows SDK 10.0.0.19041
2. 本应用使用了[CommunityToolKit中的实验性功能](https://github.com/CommunityToolkit/Labs-Windows)，请参考[Toolkit Labs](https://github.com/CommunityToolkit/Windows/wiki/Preview-Packages)的wiki将Toolkit Labs添加至Nuget的Package Sources。

**开放源代码库**  
  
除了应用已列出的代码源外，个人空间页面、部分列表样式参考了网易云第三方`LyricEase`；用户信息的右键预览、卡片样式、信息提示框和标题栏参考了[Richasy](https://github.com/Richasy)的哔哩助理。
