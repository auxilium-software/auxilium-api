from passlib.context import CryptContext

PASSWORD_CONTEXT = CryptContext(
    schemes=[
        "argon2",
        "bcrypt",
    ],
    deprecated="auto",
    argon2__memory_cost=65536,
    argon2__time_cost=3,
    argon2__parallelism=1,
)


def verify_password(plain_password: str, hashed_password: str) -> bool:
    global PASSWORD_CONTEXT
    return PASSWORD_CONTEXT.verify(plain_password, hashed_password)


def get_password_hash(password: str) -> str:
    global PASSWORD_CONTEXT
    return PASSWORD_CONTEXT.hash(password)
