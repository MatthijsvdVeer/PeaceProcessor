# Daily AI Meditations
This application is capable of generating self-guided meditation videos and uploading them to YouTube.

It's currently feeding [this YouTube Playlist][1]

```mermaid
flowchart TD
    get-topic[Get Topic]:::AI
    create-prompt[Create Prompt]
    create-script[Create Script]:::AI
    create-image-prompt[Create Image Prompt]:::AI
    generate-picture[Generate Picture]:::AI
    tts[Text To Speech]:::AI
    generate-video[Generate Video]
    upload-video[Upload To YouTube]
    generate-metadata[Generate Video Description]:::AI


    get-topic-->create-image-prompt-->generate-picture-->generate-video    
    get-topic-->create-prompt-->create-script-->tts-->generate-video-->upload-video
    create-script-->generate-metadata-->upload-video
    
    classDef AI fill:#f96
```

_Use at your own risk, it can get expensive fast!_

[1]: https://www.youtube.com/playlist?list=PLy5ORBQVkCnCmWnWzQXKiAwZ2Tv0dO9iV
