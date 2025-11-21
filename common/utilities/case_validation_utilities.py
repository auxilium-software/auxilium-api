
class CaseValidationUtilities:
    def __init__(self):
        self.expected_structure = {
            'Databases': {
                'MariaDB': {
                    'Host': str,
                    'Port': int,
                    'Username': str,
                    'Password': str,
                    'Database': str,
                },
                'CouchDB': {
                    'Protocol': str,
                    'Host': str,
                    'Port': int,
                    'Username': str,
                    'Password': str,
                    'Databases': {
                        'Cases': str,
                        'Files': str,
                        'Messages': str,
                        'Users': str,
                        'Surveys': str,
                    }
                },
                'Redis': {
                    'Host': str,
                    'Port': int,
                    'Password': str,
                    'DatabaseSlots': {
                        'Cache': int,
                    }
                },
                'RabbitMQ': {
                    'Host': str,
                    'Port': int,
                    'Username': str,
                    'Password': str,
                    'VirtualHost': str,
                    'Heartbeat': int,
                    'BlockedConnectionTimeout': int,
                    'Exchange': str,
                    'Queues': {
                        "Notifications": str,
                    },
                },
                'ClickHouse': {
                    'Host': str,
                    'Ports': {
                        "HTTP": int,
                        "TCP": int,
                    },
                    'Username': str,
                    'Password': str,
                    'Database': str,
                    'Table': str,
                    'Secure': bool,
                    'Verify': bool,
                    'Compression': bool,
                    'ConnectTimeout': int,
                    'SendReceiveTimeout': int,
                    'BatchSize': int,
                    'FlushInterval': int,
                    'MaxQueueSize': int,
                    'TTLDays': int,
                    'PartitionBy': str,
                }
            },
            'ReCAPTCHA': {
                'SiteKey': str,
                'SecretKey': str,
                'ScoreThreshold': float,
            },
            'JWT': {
                'SecretKey': str,
                'Algorithm': int,
            },
            'API': {
                'URL': str,
                'Host': str,
                'Port': int,
                'AllowedOrigins': list,
                'AllowedHosts': list,
            },
            'FileSystem': {
                "RootStorageDirectories": {
                    'AuxLFS': str,
                    'SecondaryLogs': str,
                }
            },
            'Instance': {
                'QualifiedDNS': str,
                'Branding': {
                    'Logo': str,
                    'LogoContrast': str,
                    'Name': str,
                },
                'Contacts': {
                    'Primary': {
                        'EmailAddress': str,
                        'Phone': {
                            'Number': str,
                            'OpeningHours': str,
                        },
                        'Text': {
                            'Number': str,
                            'OpeningHours': str,
                        }
                    },
                    'Maintainer': {
                        'Name': str,
                        'EmailAddress': str,
                    },
                    'GeneralEnquiries': {
                        'Name': str,
                        'EmailAddress': str,
                    }
                },
                'SignUpByInviteOnly': str,
                'ExternalOrgSignUpByInviteOnly': str,
                'StaffSignUpByInviteOnly': str,
                'DefaultTimeZone': str,
                'Navigation': {
                    'About': str|None,
                }
            },
            'NewRelic': {
                'Key': str,
            },
        }

        self.errors = []

    def validate_structure(self, data: dict, expected: dict, path: str = '') -> bool:
        # check for missing keys
        for key in expected:
            current_path = f"{path}.{key}" if path else key

            if key not in data:
                self.errors.append(f"Missing required key: {current_path}")
                continue

            expected_value = expected[key]
            actual_value = data[key]

            # If expected value is a dict, recurse
            if isinstance(expected_value, dict):
                if not isinstance(actual_value, dict):
                    self.errors.append(
                        f"Expected dict at {current_path}, got {type(actual_value).__name__}"
                    )
                else:
                    self.validate_structure(actual_value, expected_value, current_path)

            # If expected value is list type, check if actual is list
            elif expected_value is list:
                if not isinstance(actual_value, list):
                    self.errors.append(
                        f"Expected list at {current_path}, got {type(actual_value).__name__}"
                    )

            # For None, we just check that the key exists (value can be anything)
            # This is already satisfied by the key being present

        # check for extra keys
        for key in data:
            if key not in expected:
                current_path = f"{path}.{key}" if path else key
                self.errors.append(f"Unexpected key: {current_path}")

        return len(self.errors) == 0

    def validate_dict(self, data: dict) -> bool:
        self.errors = []
        return self.validate_structure(data, self.expected_structure)
