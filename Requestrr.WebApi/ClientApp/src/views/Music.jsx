
import { useEffect, useState } from "react";
import { useDispatch, useSelector } from 'react-redux';
import { Alert } from "reactstrap";
import { getSettings } from "../store/actions/MusicClientsActions";
import { saveDisabledClient } from "../store/actions/MusicClientsActions"
import { saveLidarrClient } from "../store/actions/LidarrClientActions";
import Dropdown from "../components/Inputs/Dropdown"
import MultiDropdown from "../components/Inputs/MultiDropdown"
import Lidarr from "../components/DownloadClients/Lidarr/Lidarr";

// reactstrap components
import {
  Button,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Container,
  Row,
  Col
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";

const RELEASE_TYPE_ORDER = ["Album", "EP", "Single", "Broadcast", "Other"];


function Music(props) {
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [client, setClient] = useState("");
  const [restrictions, setRestrictions] = useState("None");
  const [lidarr, setLidarr] = useState({});
  const [isLidarrValid, setIsLidarrValid] = useState(false);


  const reduxState = useSelector((state) => {
    return {
      settings: state.music
    };
  });
  const dispatch = useDispatch();


  useEffect(() => {
    dispatch(getSettings())
      .then(data => {
        setIsLoading(false);
        setClient(data.payload.client);
        setRestrictions(data.payload.restrictions || "None");
        setLidarr(data.payload.lidarr);
      });
  }, []);


  useEffect(() => {
    if (!isSubmitted)
      return;

    if (!isSaving) {
      if ((client === "Disabled"
        || (client === "Lidarr"
          && isLidarrValid)
      )) {
        setIsSaving(true);

        let saveAction = null;

        if (client === "Disabled") {
          saveAction = dispatch(saveDisabledClient());
        } else if (client === "Lidarr") {
          saveAction = dispatch(saveLidarrClient({
            lidarr: lidarr,
            restrictions: restrictions
          }));
        }

        saveAction.then(data => {
          setIsSaving(false);
          setIsSubmitted(false);

          if (data.ok) {
            setSaveAttempted(true);
            setSaveError("");
            setSaveSuccess(true);
          } else {
            var error = "An unknown error occurred while saving.";

            if (typeof (data.error) === "string")
              error = data.error;

            setSaveAttempted(true);
            setSaveError(error);
            setSaveSuccess(false);
          }
        });
      } else {
        setSaveAttempted(true);
        setSaveError("Some fields are invalid, please fix them before saving.");
        setSaveSuccess(false);
        setIsSubmitted(false);
      }
    }
  }, [isSubmitted]);


  const onClientChange = () => {
    setLidarr(reduxState.settings.lidarr);
    setRestrictions(reduxState.settings.restrictions || "None");

    setSaveAttempted(false);
    setIsSubmitted(false);
  }

  const onSaving = (e) => {
    e.preventDefault();
    setIsSubmitted(true);
  }

  const getRestrictionItems = () => {
    const categories = reduxState?.settings?.lidarr?.categories || [];
    const availableTypes = [...new Set(categories
      .flatMap(x => x.primaryTypes || [])
      .filter(x => typeof x === "string" && x.trim().length > 0))];

    const sortedTypes = availableTypes.sort((a, b) => {
      const left = RELEASE_TYPE_ORDER.indexOf(a);
      const right = RELEASE_TYPE_ORDER.indexOf(b);
      const leftOrder = left === -1 ? 999 : left;
      const rightOrder = right === -1 ? 999 : right;

      if (leftOrder !== rightOrder)
        return leftOrder - rightOrder;

      return a.localeCompare(b);
    });

    return sortedTypes.map(type => ({
      id: `Single:${type}`,
      name: `Force single ${type.toLowerCase()}`
    }));
  }

  const normalizeLegacyRestriction = (value) => {
    if (value === "SingleAlbum") return "Single:Album";
    if (value === "SingleEP") return "Single:EP";
    if (value === "SingleSingle") return "Single:Single";
    return value;
  }

  const getSelectedRestrictions = () => {
    if (!restrictions || restrictions === "None")
      return [];

    const selectedIds = restrictions
      .split(",")
      .map(x => normalizeLegacyRestriction(x.trim()))
      .filter(x => x.length > 0);

    return getRestrictionItems().filter(x => selectedIds.includes(x.id));
  }

  const onRestrictionsChanged = (values) => {
    const ids = (values || []).map(x => x.id);
    if (ids.length === 0) {
      setRestrictions("None");
      return;
    }

    setRestrictions(ids.map(normalizeLegacyRestriction).join(","));
  }




  return (
    <>
      <UserHeader title="Music" description="This page is for configuring the connection between your bot and your favorite music management client" />
      <Container className="mt--7" fluid>
        <Row>
          <Col className="order-xl-1" xl="12">
            <Card className="bg-secondary shadow">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Configuration</h3>
                  </Col>
                  <Col className="text-right" xs="4">
                    <Button
                      color="primary"
                      href="#pablo"
                      onClick={e => e.preventDefault()}
                      size="sm"
                    >
                      Settings
                    </Button>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody className={isLoading ? "fade" : "fade show"}>
                <Form className="complex">
                  <h6 className="heading-small text-muted mb-4">
                    General Settings
                  </h6>
                  <div className="pl-lg-4">
                    <Row>
                      <Col lg="6">
                        <Dropdown
                          name="Download Client"
                          value={client}
                          items={[{ name: "Disabled", value: "Disabled" }, { name: "Lidarr", value: "Lidarr" }]}
                          onChange={newClient => { setClient(newClient); onClientChange(); }}
                        />
                      </Col>
                      {
                        client !== "Disabled"
                          ? <Col lg="6">
                            <MultiDropdown
                              name="Request Restrictions"
                              placeholder="Select restriction(s)"
                              selectedItems={getSelectedRestrictions()}
                              items={getRestrictionItems()}
                              onChange={onRestrictionsChanged} />
                          </Col>
                          : null
                      }
                    </Row>
                    <Row>
                      <Col lg="6">
                        {
                          reduxState.settings.client !== client && reduxState.settings.client !== "Disabled" ?
                            <Alert className="text-center" color="warning">
                              <strong>Changing the download client will delete all pending music notifications.</strong>
                            </Alert>
                            : null
                        }
                      </Col>
                    </Row>
                  </div>
                  {
                    client !== "Disabled"
                      ? <>
                        <hr className="my-4" />
                        {
                          client == "Lidarr" ?
                            <>
                              <Lidarr
                                onChange={values => setLidarr(values)}
                                onValidate={value => setIsLidarrValid(value)}
                                isSubmitted={isSubmitted}
                                isSaving={isSaving}
                              />
                            </>
                            : null
                        }
                      </>
                      : null
                  }
                  <div className="pl-lg-4">
                    <Row>
                      <Col>
                        <FormGroup className="mt-4">
                          {
                            saveAttempted && !isSaving ?
                              !saveSuccess ? (
                                <Alert className="text-center" color="danger">
                                  <strong>{saveError}</strong>
                                </Alert>)
                                : <Alert className="text-center" color="success">
                                  <strong>Settings updated successfully.</strong>
                                </Alert>
                              : null
                          }
                        </FormGroup>
                        <FormGroup className="text-right">
                          <button className="btn btn-icon btn-3 btn-primary" onClick={onSaving} disabled={isSaving} type="button">
                            <span className="btn-inner--icon"><i className="fas fa-save"></i></span>
                            <span className="btn-inner--text">Save Changes</span>
                          </button>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                </Form>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default Music;