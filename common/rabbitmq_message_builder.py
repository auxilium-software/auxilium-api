
class RabbitMQMessageBuilder:

    @staticmethod
    def build_email(
            configuration,
            recipient_address: str,
            recipient_name: str,
            subject: str,
            txtBody: str,
            htmlBody: str,
    ) -> dict:

        if not recipient_address or '@' not in recipient_address:
            raise ValueError("Invalid recipient email address")

        if not subject or not txtBody or not htmlBody:
            raise ValueError("Subject and (both TXT and HTML) body are required")

        builder = {
            "@context": "https://schema.org/",
            "@type": "EmailMessage",
            "sender": {
                "@type": "Person",
                "name": "Auxilium Portal",
                "email": configuration.get_string('Instance', 'Contacts', 'Primary', 'EmailAddress'),
            },
            "toRecipient": {
                "@type": "Person",
                "name": recipient_name,
                "email": recipient_address,
            },
            "about": subject,
            "text": txtBody,
            'encoding': 'text/html',
            'encodingFormat': 'text/html',
            'articleBody': htmlBody,
        }
        return builder
