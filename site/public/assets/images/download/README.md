# Download page screenshot assets

This folder contains tracked public website screenshots used by the Windows Download page feature cards.

Expected files:

- `quick-start.webp`
- `topics.webp`
- `guided-lesson.webp`
- `conversation.webp`

Public CMS/default paths:

- `/assets/images/download/quick-start.webp`
- `/assets/images/download/topics.webp`
- `/assets/images/download/guided-lesson.webp`
- `/assets/images/download/conversation.webp`

Public URLs:

- `https://languagevoicetutor.com/assets/images/download/quick-start.webp`
- `https://languagevoicetutor.com/assets/images/download/topics.webp`
- `https://languagevoicetutor.com/assets/images/download/guided-lesson.webp`
- `https://languagevoicetutor.com/assets/images/download/conversation.webp`

Production server path:

- `/var/www/languagevoicetutor/site/assets/images/download/`

These files are public website assets for `download.html`; they are not installer artifacts and are not Windows release files. They are safe to upload with the static site upload flow, which uploads `site/public` website files and top-level folders such as `site/public/assets` but skips `site/public/releases/**` completely. Uploading or replacing these screenshots must not touch `site/public/releases/windows/direct/latest.json`, the public `latest.json`, `LanguageVoiceTutorSetup-1.1.exe`, or any other installer/release artifact.

Website CMS Download feature-card image fields (`featureCard1ImagePath` through `featureCard4ImagePath`) should use the public paths above. Blank or missing image paths normalize back to these defaults during the current CMS Save draft / Publish flow.
