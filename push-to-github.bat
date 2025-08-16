@echo off
echo GitHubリポジトリに接続します...
echo.
echo GitHubでリポジトリを作成後、以下のコマンドを実行してください：
echo.
echo 1. GitHubリポジトリのURLを設定:
echo    git remote add origin https://github.com/YOUR_USERNAME/mbti-civilizations-rts.git
echo.
echo 2. ブランチ名を確認してプッシュ:
echo    git branch -M main
echo    git push -u origin main
echo.
echo 注意: YOUR_USERNAMEを実際のGitHubユーザー名に置き換えてください
echo.
echo HTTPSの代わりにSSHを使用する場合:
echo    git remote add origin git@github.com:YOUR_USERNAME/mbti-civilizations-rts.git
echo.
pause