import Link from "next/link";

interface NavLinkProps {
  href: string;
  name: string;
}

function NavLink({ href, name }: NavLinkProps) {
  return (
    <Link href={href} passHref legacyBehavior>
      {name}
    </Link>
  );
}

export default NavLink;
