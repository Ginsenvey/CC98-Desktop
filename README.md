## CC98 Desktop 
现在，可以从微软商店下载本应用的最新版本,并在校外免RVPN访问论坛。增加了内置WebVPN支持，以及自定义表情包功能。采用了新的登录方式。

<p align="center">
  <a title="从 Microsoft 获取" href="https://apps.microsoft.com/detail/9NJ1LFJ8CDQ0?hl=zh-cn&gl=CN&ocid=pdpshare">
      <img src="https://get.microsoft.com/images/zh-CN%20dark.svg" width=144 />
  </a>
</p>


> CC98 Desktop是一个基于Windows App SDK开发的论坛客户端，采用FluentDesign绘制UI,旨在桌面端提供更加易于交互、触控友好、流畅快速的论坛浏览体验。
> 
> 本应用仅支持Windows10/11。Mac/Linux的跨平台版本正在早期开发阶段，将会使用新的UI风格。


**功能**
- 内置WebVPN:在校外无感访问论坛内容。

- 自定义表情:你可以搜藏图片URL，从而像表情包一样使用它。

- 更小的内存占用：相同页面下，不打开任何拓展的浏览器使用的内存是客户端的4倍以上。

- 更快：几乎所有API请求不需要等待加载。

- 更好的UBB编辑器：具备一个Office样式的UBB编辑器，界面易于使用。相比于网页端支持UBB实时预览，所见即所得。

- 完整的触控支持。相比于网页端，Surface等二合一设备应该更容易操作。

- 浏览主题帖和版块。支持渲染粗体/斜体/下划线/图片/文件/表格/代码块/链接/表情包等大多数UBB元素。

- 自动签到和抽卡

- 预览帖子中的音乐和视频，下载文件。具备一个功能完整的看图页面。

- 右键头像可预览用户信息；左键头像可进入个人空间。

- 具有“关注页面”“收藏夹”等功能，允许用户随时追踪感兴趣的版块和话题。

- 支持Win11平台的云母/亚克力效果

- 高级复制：允许用户直接复制帖子的UBB和Markdown代码

- 自定义顶部贴图

- 自定义启动页

- 无感知认证：当登录令牌过期时，进入帖子会自动刷新令牌。

- 完整的内链跳转功能。当你点击用户名/楼层/图片/主题帖链接时，快速跳转。


**Issues**

- 暂时没有针对断网/网络不稳定等问题做出处理。

- 一些页面还没有完成。比如回复私信/@/转账/Md编辑器

- 暂时只能解析一部分表情包。

- UBB解析器不能应对复杂的嵌套情况。UBB呈现器不能渲染彩色文本/Font/ReplyView/等标签。Markdown呈现器不支持内联HTML/Latex。

- 许多管理功能还未上线。

- 某些特殊的版面Slogan显示为空。
ps:如果一个按钮点不动那多半是作者没有做这个功能😎

**下载&安装**


在微软商店中安装正式版本。ARM版本会随后提供。


仓库：https://github.com/Ginsenvey/CC98-Desktop  


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

你可以在开发者的Github Issue处，或[CC98 桌面客户端的开发进度记录楼](https://www.cc98.org/topic/6173309)反馈问题。通常，前端bug修复比较快。

你也可以克隆本应用仓库，自由修改和编译新的分支。不过，在分发时，应当告知所有的改动。

**开放源代码库**

除了应用已列出的代码源外，个人空间页面、部分列表样式参考了网易云第三方`LyricEase`；用户信息的右键预览、卡片样式、信息提示框和标题栏参考了[Richasy](https://github.com/Richasy)的哔哩助理。





**从源开始构建**

1. 克隆本仓库到Visual Studio 2022.当然，仓库代码不一定是最新的。

2. 将以下Nuget源添加到vs:[https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json](https://pkgs.dev.azure.com/dotnet/CommunityToolkit/_packaging/CommunityToolkit-Labs/nuget/v3/index.json) 

3. 生成应用程序。如果报错，可以多生成几次，vs会自动补全依赖库。

4. 调试！
