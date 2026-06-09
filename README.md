# CC98 Desktop

`CC98 桌面版` 是CC98跨平台计划的一部分。 

<a title="从 Microsoft 获取" href="https://apps.microsoft.com/detail/9NJ1LFJ8CDQ0?hl=zh-cn&gl=CN&ocid=pdpshare">  
    <img src="https://get.microsoft.com/images/zh-CN%20dark.svg" />
</a>

> CC98 Desktop是一个基于Windows App SDK开发的论坛客户端，采用FluentDesign绘制UI,旨在桌面端提供更加易于交互、触控友好、流畅快速的论坛浏览体验。  
>   
> 本应用适用于搭载Windows10/11、X64和ARM64架构的计算机。

**致谢**
[@Avatar343](https://github.com/Verrickt) GG为应用重构了UBB核心解析器，[@Auser](https://github.com/sgjsakura) MM优化了源代码并提供了大量支持。

**此版本中的新功能**  
- 全新的UBB渲染器，涵盖绝大多数标签的呈现。
  
- 以轮播的方式查看帖子中的图片，就像使用微信一样。 
  
- 支持发帖添加标签 
  
- 更好的搜索面板
  
- 不再支持修改主面板的配色方案。
  
- 支持“麻将脸”表情。 
  
- 使用规范化代码重写，提高了运行稳定性。
  
- 更好的日志系统。  
   
  
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
1. 下载Visual Studio 2022及以上; 在工作负载中，安装Windows SDK 10.0.0.19041
2. 本应用使用了[CommunityToolKit中的实验性功能](https://github.com/CommunityToolkit/Labs-Windows)，请参考[Toolkit Labs](https://github.com/CommunityToolkit/Windows/wiki/Preview-Packages)的wiki将Toolkit Labs添加至Nuget的Package Sources。
3. 选择Release分支进行生成。Dev分支中包含未完成的功能和未知问题。

**开放源代码库**  
  
除了应用已列出的代码源外，个人空间页面、部分列表样式参考了网易云第三方`LyricEase`；用户信息的右键预览、卡片样式、标题栏参考了[Richasy](https://github.com/Richasy)的哔哩助理。
