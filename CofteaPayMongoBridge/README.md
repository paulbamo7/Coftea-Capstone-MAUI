# Coftea PayMongo Bridge

This minimal ASP.NET Core API lets the Coftea POS application receive PayMongo status updates even when the payment is completed on a different device.

## Features

- `/paymongo/webhook` – receives PayMongo webhook events, verifies the signature, and records the latest status for a source.
- `/payment-status/{sourceId}` – the POS can poll this endpoint to check the latest status.
- `/payment-status/back-to-pos` – optional endpoint you can call from the web success page to immediately mark a source as confirmed.

## Configuration

Set your PayMongo webhook signing secret either in `appsettings.json` or via the `PAYMONGO_WEBHOOK_SECRET` environment variable.

```jsonc
{
  "PayMongo": {
    "WebhookSecret": "whsec_..."
  }
}
```

## Running locally

```bash
cd CofteaPayMongoBridge
set PAYMONGO_WEBHOOK_SECRET=whsec_...
dotnet run
```

Expose the local server to the internet with a tunnelling tool such as ngrok:

```bash
ngrok http http://localhost:5253
```

Use the public URL from ngrok (for example, `https://your-id.ngrok.io/paymongo/webhook`) when registering the webhook in the PayMongo dashboard.

## POS integration

Inside the MAUI app, call the `GET /payment-status/{sourceId}` endpoint from `PaymentPopupViewModel.WaitForGCashCompletionAsync()` in addition to the existing PayMongo polling loop. When the bridge reports `status` = `chargeable` or `paid`, you can confirm the payment immediately.

You can also have the GitHub Pages success button call `POST /payment-status/back-to-pos` to make the bridge write a `chargeable` status, which the POS will pick up on its next poll.
