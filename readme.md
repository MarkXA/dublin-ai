# Dublin AI Bootcamp - Cognitive Services workshop

We're going to build a simple application that gets your opinion on a range of images and then predicts your opinion on other similar images.

A collection of test images can be downloaded from https://azurebootcampmxa.blob.core.windows.net/downloads/gear_images.zip, or ask for the USB stick. Alternatively, there is a list of image URLs at https://azurebootcampmxa.blob.core.windows.net/downloads/imageUrls.txt if you don't want to throw too much data around.

## Custom Vision

First we're going to set up Custom Vision to allow us to classify images.

### Set up a Custom Vision project

1. Sign in to https://customvision.ai/ with your Microsoft account.
2. Create a new project. Choose a multiclass classification project since we're assigning exactly one label to images. Choose the **General (compact)** domain - a **compact** domain means we can download the trained model for use on a device.
3. If it suggests you "move to Azure", ignore it. That's for non-trial use :)
4. Create tags named **Axes** and **Boots**.
5. Everything we're doing here can be done via the UI or the API, so let's do something with the API:

### Upload tagged images

Get the project ID, training key and training endpoint from the project settings page.

Pull up the Custom Vision developer documentation at https://docs.microsoft.com/en-us/azure/cognitive-services/custom-vision-service/home.

For .NET, Python or Java, you can find instructions for installing the relevant SDK in the **Quickstarts** section. Alternatively, use the REST endpoint directly from any language using the API documentation in the **Reference** section.

Using your development environment of choice:

1. Call the GetTags endpoint to get the tag IDs for Axes and Boots.
2. Use the CreateImagesFromUrls or CreateImagesFromData endpoints to upload 50 tagged images each of axes and boots.

Check in the Custom Vision site that your images have uploaded correctly.

### Train the model

Either call the **TrainProject** endpoint, or just hit the **Train** button in the Custom Vision site. Once complete, make the latest iteration the default by either using the **GetIterations** and **UpdateIteration** endpoints or clicking the **Make default** link in the Custom Vision site.

### Predict the label of an image using the API

Use the PredictImage or PredictImageUrl endpoint to check that images of axes or boots that you haven't previously uploaded are correctly classified.

## Text Analytics

We'll be using the sentiment analysis feature of Text Analytics to get a score in the range 0-1 of how positive a statement is.

### Set up Text Analytics

In the Azure Portal, add a new resource and select *Text Analytics* from the *AI + Machine Learning* section. Choose the free tier (F0).

### Get the sentiment of a phrase

1. Get the API key and endpoint from the Overview section of the Text Analytics service in the Azure Portal.
2. Go to the documentation at https://docs.microsoft.com/en-us/azure/cognitive-services/text-analytics/ and follow the links for your language.
3. Use the Sentiment endpoint to determine the positive or negative sentiment of a phrase of your choice.

## Combine the two

For (say) 15 images of boots, enter a phrase that says what you think of each one. Use sentiment analysis to tag the boots as Good or Bad, then see if you can predict whether or not you'll like other boots.

## Bonus exercise

Since we used a compact model, you can export it for use on a device in CoreML (iOS), TensorFlow (Android), ONNX (Windows) or Docker format. Try downloading the model as a Docker file, running it on your laptop and predicting an image.